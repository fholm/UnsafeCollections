using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnsafeCollections;
using UnsafeCollections.Collections.Unsafe.Concurrent;

namespace UnsafeCollectionsTests.Unsafe.Concurrent
{
    public unsafe class UnsafeMPSCQueueTests
    {
        private static void SplitQueue(UnsafeMPSCQueue* q)
        {
            //Wrap tail back to 0
            for (int i = 0; i < 5; i++)
                UnsafeMPSCQueue.TryEnqueue(q, 111);

            //First half
            for (int i = 0; i < 5; i++)
                UnsafeMPSCQueue.TryEnqueue(q, i);

            //Move head by 5
            for (int i = 0; i < 5; i++)
                UnsafeMPSCQueue.Dequeue<int>(q);

            //Second half (head and tail are now both 5)
            for (int i = 5; i < 10; i++)
                UnsafeMPSCQueue.TryEnqueue(q, i);

            //Circular buffer now "ends" in the middle of the underlying array
        }


        [Test]
        public void ConstructorTest()
        {
            var q = UnsafeMPSCQueue.Allocate<int>(10);

            Assert.AreEqual(0, UnsafeMPSCQueue.GetCount(q));
            Assert.AreEqual(16, UnsafeMPSCQueue.GetCapacity(q));

            UnsafeMPSCQueue.Free(q);
        }

        [Test]
        public void EnqueueTest()
        {
            var q = UnsafeMPSCQueue.Allocate<int>(10);

            for (int i = 0; i < 10; i++)
            {
                UnsafeMPSCQueue.TryEnqueue(q, i * i);
            }

            Assert.AreEqual(10, UnsafeMPSCQueue.GetCount(q));
            Assert.AreEqual(16, UnsafeMPSCQueue.GetCapacity(q));

            UnsafeMPSCQueue.Clear(q);

            Assert.AreEqual(0, UnsafeMPSCQueue.GetCount(q));
            Assert.AreEqual(16, UnsafeMPSCQueue.GetCapacity(q));

            UnsafeMPSCQueue.Free(q);
        }

        [Test]
        public void DequeueTest()
        {
            var q = UnsafeMPSCQueue.Allocate<int>(10);

            for (int i = 0; i < 10; i++)
                UnsafeMPSCQueue.TryEnqueue(q, i * i);


            for (int i = 0; i < 10; i++)
            {
                int num = UnsafeMPSCQueue.Dequeue<int>(q);
                Assert.AreEqual(i * i, num);
            }

            UnsafeMPSCQueue.Free(q);
        }

        [Test]
        public void PeekTest()
        {
            var q = UnsafeMPSCQueue.Allocate<int>(10);

            for (int i = 0; i < 10; i++)
                UnsafeMPSCQueue.TryEnqueue(q, (int)Math.Pow(i + 2, 2));

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(4, UnsafeMPSCQueue.Peek<int>(q));
            }

            //Verify no items are dequeued
            Assert.AreEqual(10, UnsafeMPSCQueue.GetCount(q));

            UnsafeMPSCQueue.Free(q);
        }

        [Test]
        public void ExpandTest()
        {
            var q = UnsafeMPSCQueue.Allocate<int>(10);

            SplitQueue(q);

            //Fill buffer to capacity.
            for (int i = 0; i < 6; i++)
                UnsafeMPSCQueue.TryEnqueue(q, 999);


            //Buffer is full, can no longer insert.
            Assert.IsFalse(UnsafeMPSCQueue.TryEnqueue(q, 10));

            UnsafeMPSCQueue.Free(q);
        }

        [Test]
        public void TryActionTest()
        {
            var q = UnsafeMPSCQueue.Allocate<int>(16);

            //Inserts 10 items.
            SplitQueue(q);
            var arr = UnsafeMPSCQueue.ToArray<int>(q);
            //Insert 6 more to fill the queue
            for (int i = 0; i < 6; i++)
                UnsafeMPSCQueue.TryEnqueue(q, 999);

            arr = UnsafeMPSCQueue.ToArray<int>(q);
            Assert.IsFalse(UnsafeMPSCQueue.TryEnqueue(q, 10));
            Assert.IsTrue(UnsafeMPSCQueue.TryPeek(q, out int result));
            Assert.AreEqual(0, result);

            for (int i = 0; i < 10; i++)
            {
                Assert.IsTrue(UnsafeMPSCQueue.TryDequeue(q, out int val));
                Assert.AreEqual(i, val);
            }

            //Empty 6 last items
            for (int i = 0; i < 6; i++)
                Assert.IsTrue(UnsafeMPSCQueue.TryDequeue(q, out int val));

            //Empty queue
            Assert.IsFalse(UnsafeMPSCQueue.TryPeek(q, out int res));

            UnsafeMPSCQueue.Free(q);
        }

        [Test]
        public void ClearTest()
        {
            var q = UnsafeMPSCQueue.Allocate<int>(16);

            //Inserts 10 items.
            SplitQueue(q);

            Assert.AreEqual(10, UnsafeMPSCQueue.GetCount(q));
            UnsafeMPSCQueue.Clear(q);
            Assert.AreEqual(0, UnsafeMPSCQueue.GetCount(q));

            Assert.IsTrue(UnsafeMPSCQueue.IsEmpty<int>(q));

            UnsafeMPSCQueue.Free(q);
        }

        [Test]
        public void IteratorTest()
        {
            var q = UnsafeMPSCQueue.Allocate<int>(10);

            //Wrap tail around
            SplitQueue(q);

            //Iterator should start from the head.
            int num = 0;
            foreach (int i in UnsafeMPSCQueue.GetEnumerator<int>(q))
            {
                Assert.AreEqual(num, i);
                num++;
            }

            UnsafeMPSCQueue.Free(q);
        }

#if DEBUG
        [Test]
        public void InvalidTypeTest()
        {
            var q = UnsafeMPSCQueue.Allocate<int>(10);

            Assert.Catch<AssertException>(() => { UnsafeMPSCQueue.TryEnqueue<float>(q, 162); });

            UnsafeMPSCQueue.Free(q);
        }
#endif

        [Test]
        public void CopyToTest()
        {
            var q = UnsafeMPSCQueue.Allocate<int>(10);
            SplitQueue(q);

            var arr = UnsafeMPSCQueue.ToArray<int>(q);

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(i, arr[i]);
            }
        }

        //[Test]
        //Demonstration that this queue is SPSC
        public void ConcurrencyTest()
        {
            var q = UnsafeMPSCQueue.Allocate<int>(16);
            int count = 10000;


            Thread reader = new Thread(() =>
            {
                for (int i = 0; i < count; i++)
                {
                    var num = UnsafeMPSCQueue.Dequeue<int>(q);
                    //Assert.AreEqual(i, num)
                    if (i != num)
                    {
                        Assert.Fail("Dequeue gave an unexpected result");
                    }
                }
            });

            reader.Start();

            for (int i = 0; i < count;)
            {
                if (UnsafeMPSCQueue.TryEnqueue(q, i))
                    i++;
            }

            reader.Join();

            UnsafeMPSCQueue.Free(q);
        }

        [Test]
        // Demonstration that this queue is MPSC
        public void ConcurrencyTest2()
        {
            var q = UnsafeMPSCQueue.Allocate<int>(16000);
            int count = 10000;

            Thread writer = new Thread(() =>
            {
                for (int i = 0; i < count / 2;)
                    if (UnsafeMPSCQueue.TryEnqueue(q, i))
                        i++;
            });

            Thread writer2 = new Thread(() =>
            {
                for (int i = 0; i < count / 2;)
                    if (UnsafeMPSCQueue.TryEnqueue(q, i))
                        i++;
            });

            writer.Start();
            writer2.Start();


            writer.Join();
            writer2.Join();

            Assert.AreEqual(count, UnsafeMPSCQueue.GetCount(q));

            UnsafeMPSCQueue.Free(q);
        }
    }
}
