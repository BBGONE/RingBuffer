# RingBuffer
Ring Buffer class written in C# and Typescript. It works as a queue (FIFO) with oldest items eviction.


Ideal for a queue, where oldest unconsumed items can be lost (evicted) in case when a producer producers items at a faster rate than they are consumed.
The Circular buffer prevents growing the queue to large sizes and also prevent hampering the producer performance (it just evicts oldest unconsumed items).
It's good, for example, for live images streams (video), when some images can be lost without problems.
