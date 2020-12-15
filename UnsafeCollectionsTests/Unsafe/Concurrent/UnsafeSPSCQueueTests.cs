using NUnit.Framework;
using System;
using System.Threading;
using UnsafeCollections;
using UnsafeCollections.Collections.Unsafe.Concurrent;

namespace UnsafeCollectionsTests.Unsafe.Concurrent
{
    public unsafe class UnsafeSPSCQueueTests
    {
        private static void SplitQueue(UnsafeSPSCQueue* q)
        {
            //Wrap tail back to 0
            for (int i = 0; i < 5; i++)
                UnsafeSPSCQueue.Enqueue(q, 111);

            //First half
            for (int i = 0; i < 5; i++)
                UnsafeSPSCQueue.Enqueue(q, i);

            //Move head by 5
            for (int i = 0; i < 5; i++)
                UnsafeSPSCQueue.Dequeue<int>(q);

            //Second half (head and tail are now both 5)
            for (int i = 5; i < 10; i++)
                UnsafeSPSCQueue.Enqueue(q, i);

            //Circular buffer now "ends" in the middle of the underlying array
        }


        [Test]
        public void ConstructorTest()
        {
            var q = UnsafeSPSCQueue.Allocate<int>(10);

            Assert.AreEqual(0, UnsafeSPSCQueue.GetCount(q));
            Assert.AreEqual(16, UnsafeSPSCQueue.GetCapacity(q));

            UnsafeSPSCQueue.Free(q);
        }

        [Test]
        public void EnqueueTest()
        {
            var q = UnsafeSPSCQueue.Allocate<int>(10);

            for (int i = 0; i < 10; i++)
            {
                UnsafeSPSCQueue.Enqueue(q, i * i);
            }

            Assert.AreEqual(10, UnsafeSPSCQueue.GetCount(q));
            Assert.AreEqual(16, UnsafeSPSCQueue.GetCapacity(q));

            UnsafeSPSCQueue.Clear(q);

            Assert.AreEqual(0, UnsafeSPSCQueue.GetCount(q));
            Assert.AreEqual(16, UnsafeSPSCQueue.GetCapacity(q));

            UnsafeSPSCQueue.Free(q);
        }

        [Test]
        public void DequeueTest()
        {
            var q = UnsafeSPSCQueue.Allocate<int>(10);

            for (int i = 0; i < 10; i++)
                UnsafeSPSCQueue.Enqueue(q, i * i);


            for (int i = 0; i < 10; i++)
            {
                int num = UnsafeSPSCQueue.Dequeue<int>(q);
                Assert.AreEqual(i * i, num);
            }

            UnsafeSPSCQueue.Free(q);
        }

        [Test]
        public void PeekTest()
        {
            var q = UnsafeSPSCQueue.Allocate<int>(10);

            for (int i = 0; i < 10; i++)
                UnsafeSPSCQueue.Enqueue(q, (int)Math.Pow(i + 2, 2));

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(4, UnsafeSPSCQueue.Peek<int>(q));
            }

            //Verify no items are dequeued
            Assert.AreEqual(10, UnsafeSPSCQueue.GetCount(q));

            UnsafeSPSCQueue.Free(q);
        }

        [Test]
        public void ExpandTest()
        {
            var q = UnsafeSPSCQueue.Allocate<int>(10);

            SplitQueue(q);

            //Fill buffer to capacity.
            for (int i = 0; i < 6; i++)
                UnsafeSPSCQueue.Enqueue(q, 999);


            //Buffer is full, can no longer insert.
            Assert.IsFalse(UnsafeSPSCQueue.TryEnqueue(q, 10));

            UnsafeSPSCQueue.Free(q);
        }

        [Test]
        public void TryActionTest()
        {
            var q = UnsafeSPSCQueue.Allocate<int>(16);

            //Inserts 10 items.
            SplitQueue(q);

            //Insert 6 more to fill the queue
            for (int i = 0; i < 6; i++)
                UnsafeSPSCQueue.TryEnqueue(q, 999);

            Assert.IsFalse(UnsafeSPSCQueue.TryEnqueue(q, 10));
            Assert.IsTrue(UnsafeSPSCQueue.TryPeek(q, out int result));
            Assert.AreEqual(0, result);

            for (int i = 0; i < 10; i++)
            {
                Assert.IsTrue(UnsafeSPSCQueue.TryDequeue(q, out int val));
                Assert.AreEqual(i, val);
            }

            //Empty 6 last items
            for (int i = 0; i < 6; i++)
                Assert.IsTrue(UnsafeSPSCQueue.TryDequeue(q, out int val));

            //Empty queue
            Assert.IsFalse(UnsafeSPSCQueue.TryPeek(q, out int res));

            UnsafeSPSCQueue.Free(q);
        }

        [Test]
        public void ClearTest()
        {
            var q = UnsafeSPSCQueue.Allocate<int>(16);

            //Inserts 10 items.
            SplitQueue(q);

            Assert.AreEqual(10, UnsafeSPSCQueue.GetCount(q));
            UnsafeSPSCQueue.Clear(q);
            Assert.AreEqual(0, UnsafeSPSCQueue.GetCount(q));

            Assert.IsTrue(UnsafeSPSCQueue.IsEmpty<int>(q));

            UnsafeSPSCQueue.Free(q);
        }

        [Test]
        public void IteratorTest()
        {
            var q = UnsafeSPSCQueue.Allocate<int>(10);

            // Wrap tail around
            SplitQueue(q);

            // Iterator should start from the head.
            int num = 0;
            foreach (int i in UnsafeSPSCQueue.GetEnumerator<int>(q))
            {
                Assert.AreEqual(num, i);
                num++;
            }

            // Iterated 10 items
            Assert.AreEqual(10, num);

            UnsafeSPSCQueue.Free(q);
        }

#if DEBUG
        [Test]
        public void InvalidTypeTest()
        {
            var q = UnsafeSPSCQueue.Allocate<int>(10);

            Assert.Catch<AssertException>(() => { UnsafeSPSCQueue.Enqueue<float>(q, 162); });

            UnsafeSPSCQueue.Free(q);
        }
#endif

        [Test]
        public void CopyToTest()
        {
            var q = UnsafeSPSCQueue.Allocate<int>(10);
            SplitQueue(q);

            var arr = UnsafeSPSCQueue.ToArray<int>(q);

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(i, arr[i]);
            }
        }

        //[Test]
        // Demonstration that this queue is SPSC
        public void ConcurrencyTest()
        {
            var q = UnsafeSPSCQueue.Allocate<int>(16);
            int count = 10000;

            Thread reader = new Thread(() =>
            {
                for (int i = 0; i < count; i++)
                {
                    if (i != UnsafeSPSCQueue.Dequeue<int>(q))
                        Assert.Fail("Dequeue gave an unexpected result");
                }
            });

            reader.Start();

            for (int i = 0; i < count; i++)
            {
                UnsafeSPSCQueue.Enqueue(q, i);
            }

            reader.Join();

            UnsafeSPSCQueue.Free(q);
        }

        //[Test]
        //Demonstration that this queue isn't MPSC
        public void ConcurrencyTest2()
        {
            var q = UnsafeSPSCQueue.Allocate<int>(16000);
            int count = 10000;

            Thread writer = new Thread(() =>
            {
                for (int i = 0; i < count / 2; i++)
                    UnsafeSPSCQueue.Enqueue(q, i);
            });
            Thread writer2 = new Thread(() =>
            {
                for (int i = 0; i < count / 2; i++)
                    UnsafeSPSCQueue.Enqueue(q, i);
            });

            writer.Start();
            writer2.Start();


            writer.Join();
            writer2.Join();

            Assert.AreNotEqual(count, UnsafeSPSCQueue.GetCount(q));

            UnsafeSPSCQueue.Free(q);
        }
    }
}
