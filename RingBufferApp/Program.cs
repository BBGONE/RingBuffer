using System;

namespace RingBufferApp
{
    class Program
    {
        static void Main(string[] args)
        {
            RingBuffer<string> ringBuffer = new RingBuffer<string>(5, (val) => {
                Console.WriteLine($"evicted: {val}");
            });

            for(int i = 0; i < 10; ++i)
            {
                ringBuffer.Enq(i.ToString());
            }

            var peeks = ringBuffer.PeekN(5);

            foreach(var p in peeks)
            {
                Console.WriteLine($"peek: {p}");
            }

            var deqs = ringBuffer.DeqN(10);

            foreach (var d in deqs)
            {
                Console.WriteLine($"deq: {d}");
            }
            

            for (int i = 0; i < 5; ++i)
            {
                string value = ringBuffer.Deq();
                Console.WriteLine(value?? "null");
            }

            Console.WriteLine("Hello World!");

        }
    }
}
