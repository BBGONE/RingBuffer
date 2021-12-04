# RingBuffer
Ring Buffer class written in C# and Typescript. It works as a queue (FIFO) with oldest items eviction.


Ideal for a disruptor pattern, where oldest unconsumed items can be lost (evicted) in case when producer producers items at a faster rate that they are consumed.
Circular buffer prevents growing the queue to large sizes and also prevent hampering producer performance.
It's good, for example, for live images streams (video), when some images can be lost without problems.
