using System;
using System.Collections.Generic;
using System.Linq;

namespace RingBufferApp
{
    /// <summary>
    /// Кольцевой буфер
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RingBuffer<T>
        where T: class
    {
        #region Поля

        private readonly object SyncLock = new object();
        private int _first = 0;
        private volatile int _size = 0;
        private T[] _items;
        private readonly Action<T> _evictedCb;
        private readonly int _capacity;

        #endregion

        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="capacity"></param>
        /// <param name="evictedCb"></param>
        public RingBuffer(int capacity = 10,  Action<T> evictedCb = null)
        {
            this._capacity = capacity;
            this._evictedCb = evictedCb;
            this._items = new T[capacity];
        }

        #region Свойства

        // вместимость буфера
        public int capacity => this._capacity;

        // пустой ли буфер
        public bool isEmpty
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
        public bool isFull
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
        public int size => this._size;

        #endregion

        #region Методы

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
        public T peek()
        {
            lock (this.SyncLock)
            {
                if (this.isEmpty)
                {
                    return null;
                }

                return this._items[this._first];
            }
        }

        // Метод возвращает запрашиваемое число элементов с начала буфера (без удаления)
        public IEnumerable<T> peekN(int count)
        {
            return this._peekN(count, false);
        }

        // Метод получения элемента из буфера
        public T deq()
        {
            lock (this.SyncLock)
            {
                T res = this.peek();

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
        public IEnumerable<T> deqN(int count) {
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
        public int enq(T item)
        {
            lock (this.SyncLock)
            {
                int end = (this._first + this._size) % this._capacity;
                bool full = this.isFull;

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

                return this._size;
            }
        }


        // Метод удаления всех элементов из буфера
        public void clear()
        {
            lock (this.SyncLock)
            {
                this._items = new T[this.capacity];
                this._first = 0;
                this._size = 0;
            }
        }

        #endregion
    }
}
