using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RingBufferApp
{
    /// <summary>
    /// Интерфейс реализуемый кольцнвым буффером
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IRingBuffer<T> : IDisposable
        where T : class
    {
        int Capacity { get; }
        bool IsEmpty { get; }
        bool IsFull { get; }
        int Size { get; }

        void Clear(bool isDisposing = false);
        T Deq();
        IEnumerable<T> DeqAll();
        IEnumerable<T> DeqN(int count);
        int Enq(T item);
        T Peek();
        IEnumerable<T> PeekN(int count);
        Task WaitForNewItems(CancellationToken cancellationToken = default);
    }
}