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
                ringBuffer.enq(i.ToString());
            }

            var peeks = ringBuffer.peekN(5);

            foreach(var p in peeks)
            {
                Console.WriteLine($"peek: {p}");
            }

            var deqs = ringBuffer.deqN(10);

            foreach (var d in deqs)
            {
                Console.WriteLine($"deq: {d}");
            }
            

            for (int i = 0; i < 5; ++i)
            {
                string value = ringBuffer.deq();
                Console.WriteLine(value?? "null");
            }

            Console.WriteLine("Hello World!");

        }
    }
}
