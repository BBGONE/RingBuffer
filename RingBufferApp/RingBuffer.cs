using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RingBufferApp
{
    /// <summary>
    /// Кольцевой буфер
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RingBuffer<T> : IRingBuffer<T>
        where T : class
    {
        #region Поля

        private readonly object SyncLock = new object();
        private int _first = 0;
        private volatile int _size = 0;
        private T[] _items;
        private readonly Action<T> _evictedCb;
        private readonly int _capacity;
        private TaskCompletionSource _taskCompletionSource;
        private CancellationTokenRegistration _cancellationTokenRegistration;
        #endregion

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="evictedCb"></param>
        public RingBuffer(int capacity = 10, Action<T> evictedCb = null)
        {
            this._capacity = capacity;
            this._evictedCb = evictedCb;
            this._items = new T[capacity];
        }

        #region Свойства

        // вместимость буфера
        public int Capacity => this._capacity;

        // пустой ли буфер
        public bool IsEmpty
        {
            get
            {
                lock (this.SyncLock)
                {
                    return this._size == 0;
                }
            }
        }

        // полный ли буфер
        public bool IsFull
        {
            get
            {
                lock (this.SyncLock)
                {
                    return this._size == this._capacity;
                }
            }
        }

        // текущее кол-во элементов в буфере
        public int Size => this._size;

        #endregion

        #region Методы

        /// <summary>
        /// Ожидания когда в буфере появятьса новые элементы
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task WaitForNewItems(CancellationToken cancellationToken = default)
        {
            lock (this.SyncLock)
            {
                if (_taskCompletionSource != null)
                {
                    return _taskCompletionSource.Task;
                }

                if (this.Size > 0)
                {
                    return Task.CompletedTask;
                }

                if (_taskCompletionSource == null)
                {
                    _taskCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

                    if (cancellationToken != CancellationToken.None)
                    {
                        _cancellationTokenRegistration = cancellationToken.Register(() => _taskCompletionSource?.TrySetCanceled());
                    }
                }

                return _taskCompletionSource.Task;
            }
        }

        // Метод возвращает запрашиваемое число элементов с начала буфера (без удаления)
        private IEnumerable<T> _peekN(int count, bool clearReturned)
        {
            lock (this.SyncLock)
            {
                if (count > this._size)
                {
                    count = this._size;
                }

                if (count == 0)
                {
                    return new T[0];
                }

                int end1 = Math.Min(this._first + count, this._capacity);
                int takeCount = end1 - this._first;
                T[] firstHalf = new Span<T>(this._items).Slice(this._first, takeCount).ToArray();

                if (clearReturned)
                {
                    for (int i = this._first; i < end1; ++i)
                    {
                        this._items[i] = null;
                    }
                }

                if (end1 < this._capacity)
                {
                    return firstHalf.ToArray();
                }

                int end2 = count - firstHalf.Length;
                T[] secondHalf = new Span<T>(this._items).Slice(0, end2).ToArray();

                if (clearReturned)
                {
                    for (int i = 0; i < end2; ++i)
                    {
                        this._items[i] = null;
                    }
                }

                return firstHalf.Concat(secondHalf).ToArray();
            }
        }

        // Метод возвращает элемент с начала буфера (без удаления)
        public T Peek()
        {
            lock (this.SyncLock)
            {
                if (this.IsEmpty)
                {
                    return null;
                }

                return this._items[this._first];
            }
        }

        // Метод возвращает запрашиваемое число элементов с начала буфера (без удаления)
        public IEnumerable<T> PeekN(int count)
        {
            return this._peekN(count, false);
        }

        // Метод получения элемента из буфера
        public T Deq()
        {
            lock (this.SyncLock)
            {
                T res = this.Peek();

                if (res != null)
                {
                    this._items[this._first] = null;
                    this._size--;
                    this._first = (this._first + 1) % this._capacity;
                    return res;
                }

                return res;
            }
        }

        // Метод получения элементов из буфера
        public IEnumerable<T> DeqN(int count)
        {
            lock (this.SyncLock)
            {
                IEnumerable<T> res = this._peekN(count, true);
                count = res.Count();
                this._size -= count;
                this._first = (this._first + count) % this._capacity;

                return res;
            }
        }


        // Метод помещения элемента в буфер
        // возвращает текущий размер буфера
        public int Enq(T item)
        {
            lock (this.SyncLock)
            {
                int end = (this._first + this._size) % this._capacity;
                bool full = this.IsFull;

                if (full)
                {
                    this._evictedCb?.Invoke(this._items[end]);
                }
                this._items[end] = item;

                if (full)
                {
                    this._first = (this._first + 1) % this._capacity;
                }
                else
                {
                    this._size++;
                }

                if (_taskCompletionSource != null && this._size > 0)
                {
                    _taskCompletionSource.TrySetResult();
                    _taskCompletionSource = null;
                    _cancellationTokenRegistration.Dispose();
                }

                return this._size;
            }
        }


        // Метод удаления всех элементов из буфера
        public void Clear(bool isDisposing = false)
        {
            lock (this.SyncLock)
            {
                this._items = new T[this.Capacity];
                this._first = 0;
                this._size = 0;

                if (isDisposing)
                {
                    if (_taskCompletionSource != null && this._size > 0)
                    {
                        _taskCompletionSource.TrySetCanceled();
                        _taskCompletionSource = null;
                        _cancellationTokenRegistration.Dispose();
                    }
                }
            }
        }

        public void Dispose()
        {
            this.Clear(true);
        }

        #endregion
    }
}
