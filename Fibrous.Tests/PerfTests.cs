﻿namespace Fibrous.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using Fibrous.Channels;
    using Fibrous.Fibers;
    using Fibrous.Fibers.Queues;
    using NUnit.Framework;

    public class PerfExecutor : IExecutor
    {
        public void Execute(IEnumerable<Action> toExecute)
        {
            //    Console.WriteLine("execute_more");
            int count = 0;
            foreach (Action action in toExecute)
            {
                action();
                count++;
            }
            if (count < 10000)
                Thread.Sleep(1);
        }

        public void Execute(Action toExecute)
        {
            //   Console.WriteLine("execute");
            toExecute();
        }
    }

    public struct MsgStruct
    {
        public int count;
    }

    [TestFixture]
    public class PerfTests
    {
        //[Test]
        //public void TestDisruptor()
        //{
        //    // RunQueue(new BusyWaitQueue(new PerfExecutor(), 100000, 30000));
        //    PointToPointPerfTestWithStruct(new ThreadFiber(new DisruptorQueue(new PerfExecutor())));
        //    PointToPointPerfTestWithInt(new ThreadFiber(new DisruptorQueue(new PerfExecutor())));
        //    PointToPointPerfTestWithObject(new ThreadFiber(new DisruptorQueue(new PerfExecutor())));
        //}
        //[Test]
        //public void TestBufferBlock()
        //{
        //    PointToPointPerfTestWithStruct(new BufferBlockQueue(new PerfExecutor()));
        //    PointToPointPerfTestWithInt(new BufferBlockQueue(new PerfExecutor()));
        //    PointToPointPerfTestWithObject(new BufferBlockQueue(new PerfExecutor()));
        //    //     RunQueue(new BufferBlockQueue(new PerfExecutor()));
        //}
        private static void PointToPointPerfTestWithStruct(IFiber fiber)
        {
            using (fiber)
            {
                fiber.Start();
                IChannel<MsgStruct> channel = new Channel<MsgStruct>();
                const int max = 5000000;
                var reset = new AutoResetEvent(false);
                var counter = new Counter(reset, max);
                channel.Subscribe(fiber, counter.OnMsg);
                Thread.Sleep(100);
                using (new PerfTimer(max))
                {
                    for (int i = 0; i <= max; i++)
                        channel.Publish(new MsgStruct { count = i });
                    Assert.IsTrue(reset.WaitOne(30000, false));
                }
            }
        }

        private class Counter
        {
            private readonly int _cutoff;
            private readonly AutoResetEvent handle;

            public Counter(AutoResetEvent handle, int cutoff)
            {
                this.handle = handle;
                _cutoff = cutoff;
            }

            public int Count { get; set; }

            public void OnMsg(MsgStruct msg)
            {
                Count++;
                if (Count == _cutoff)
                    handle.Set();
            }
        }

        private class CounterInt
        {
            private readonly int _cutoff;
            private readonly AutoResetEvent handle;

            public CounterInt(AutoResetEvent handle, int cutoff)
            {
                this.handle = handle;
                _cutoff = cutoff;
            }

            public void OnMsg(int msg)
            {
                if (msg == _cutoff)
                    handle.Set();
            }
        }

        //[Test, Explicit]
        public void PointToPointPerfTestWithInt(IFiber fiber)
        {
            using (fiber)
            {
                fiber.Start();
                var channel = new Channel<int>();
                const int max = 5000000;
                var reset = new AutoResetEvent(false);
                var counter = new CounterInt(reset, max);
                channel.Subscribe(fiber, counter.OnMsg);
                Thread.Sleep(100);
                using (new PerfTimer(max))
                {
                    for (int i = 0; i <= max; i++)
                        channel.Publish(i);
                    Assert.IsTrue(reset.WaitOne(30000, false));
                }
            }
        }

        //    [Test, Explicit]
        public void PointToPointPerfTestWithObject(IFiber fiber)
        {
            using (fiber)
            {
                fiber.Start();
                var channel = new Channel<object>();
                const int max = 5000000;
                var reset = new AutoResetEvent(false);
                var end = new object();
                Action<object> onMsg = delegate(object msg)
                {
                    if (msg == end)
                        reset.Set();
                };
                channel.Subscribe(fiber, onMsg);
                Thread.Sleep(100);
                using (new PerfTimer(max))
                {
                    var msg = new object();
                    for (int i = 0; i <= max; i++)
                        channel.Publish(msg);
                    channel.Publish(end);
                    Assert.IsTrue(reset.WaitOne(30000, false));
                }
            }
        }

        [Test]
        [Explicit]
        public void TestBoundedQueue()
        {
            PointToPointPerfTestWithStruct(new ThreadFiber(new BoundedQueue(new PerfExecutor(), 10000, 1000)));
            PointToPointPerfTestWithInt(new ThreadFiber(new BoundedQueue(new PerfExecutor(), 10000, 1000)));
            PointToPointPerfTestWithObject(new ThreadFiber(new BoundedQueue(new PerfExecutor(), 10000, 1000)));
        }

        [Test]
        [Explicit]
        public void TestBusyWait()
        {
            PointToPointPerfTestWithStruct(new ThreadFiber(new BusyWaitQueue(new PerfExecutor(), 1000, 25)));
            PointToPointPerfTestWithInt(new ThreadFiber(new BusyWaitQueue(new PerfExecutor(), 1000, 25)));
            PointToPointPerfTestWithObject(new ThreadFiber(new BusyWaitQueue(new PerfExecutor(), 1000, 25)));
        }

        [Test]
        [Explicit]
        public void TestDefault()
        {
            PointToPointPerfTestWithStruct(new ThreadFiber(new DefaultQueue(new PerfExecutor())));
            PointToPointPerfTestWithInt(new ThreadFiber(new DefaultQueue(new PerfExecutor())));
            PointToPointPerfTestWithObject(new ThreadFiber(new DefaultQueue(new PerfExecutor())));
        }

        [Test]
        [Explicit]
        public void TestPool()
        {
            PointToPointPerfTestWithStruct(new PoolFiber(new PerfExecutor()));
            PointToPointPerfTestWithInt(new PoolFiber(new PerfExecutor()));
            PointToPointPerfTestWithObject(new PoolFiber(new PerfExecutor()));
        }
    }
}