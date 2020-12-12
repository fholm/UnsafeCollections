using NUnit.Framework;
using System;
using System.Diagnostics;
using System.Threading;
using UnsafeCollections;
using UnsafeCollections.Collections.Unsafe.Concurrent;

namespace UnsafeCollectionsTests.Unsafe.Concurrent
{
    public unsafe class UnsafeSPSCRingbufferTests
    {
        private static void SplitQueue(UnsafeSPSCRingbuffer* q)
        {
            //Wrap tail back to 0
            for (int i = 0; i < 5; i++)
                UnsafeSPSCRingbuffer.Enqueue(q, 111);

            //First half
            for (int i = 0; i < 5; i++)
                UnsafeSPSCRingbuffer.Enqueue(q, i);

            //Move head by 5
            for (int i = 0; i < 5; i++)
                UnsafeSPSCRingbuffer.Dequeue<int>(q);

            //Second half (head and tail are now both 5)
            for (int i = 5; i < 10; i++)
                UnsafeSPSCRingbuffer.Enqueue(q, i);

            //Circular buffer now "ends" in the middle of the underlying array
        }


        [Test]
        public void ConstructorTest()
        {
            var q = UnsafeSPSCRingbuffer.Allocate<int>(10);

            Assert.AreEqual(0, UnsafeSPSCRingbuffer.GetCount(q));
            Assert.AreEqual(16, UnsafeSPSCRingbuffer.GetCapacity(q));

            UnsafeSPSCRingbuffer.Free(q);
        }

        [Test]
        public void EnqueueTest()
        {
            var q = UnsafeSPSCRingbuffer.Allocate<int>(10);

            for (int i = 0; i < 10; i++)
            {
                UnsafeSPSCRingbuffer.Enqueue(q, i * i);
            }

            Assert.AreEqual(10, UnsafeSPSCRingbuffer.GetCount(q));
            Assert.AreEqual(16, UnsafeSPSCRingbuffer.GetCapacity(q));

            UnsafeSPSCRingbuffer.Clear(q);

            Assert.AreEqual(0, UnsafeSPSCRingbuffer.GetCount(q));
            Assert.AreEqual(16, UnsafeSPSCRingbuffer.GetCapacity(q));

            UnsafeSPSCRingbuffer.Free(q);
        }

        [Test]
        public void DequeueTest()
        {
            var q = UnsafeSPSCRingbuffer.Allocate<int>(10);

            for (int i = 0; i < 10; i++)
                UnsafeSPSCRingbuffer.Enqueue(q, i * i);


            for (int i = 0; i < 10; i++)
            {
                int num = UnsafeSPSCRingbuffer.Dequeue<int>(q);
                Assert.AreEqual(i * i, num);
            }

            UnsafeSPSCRingbuffer.Free(q);
        }

        [Test]
        public void PeekTest()
        {
            var q = UnsafeSPSCRingbuffer.Allocate<int>(10);

            for (int i = 0; i < 10; i++)
                UnsafeSPSCRingbuffer.Enqueue(q, (int)Math.Pow(i + 2, 2));

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(4, UnsafeSPSCRingbuffer.Peek<int>(q));
            }

            //Verify no items are dequeued
            Assert.AreEqual(10, UnsafeSPSCRingbuffer.GetCount(q));

            UnsafeSPSCRingbuffer.Free(q);
        }

        [Test]
        public void ExpandTest()
        {
            var q = UnsafeSPSCRingbuffer.Allocate<int>(10);

            SplitQueue(q);

            //Fill buffer to capacity.
            for (int i = 0; i < 6; i++)
                UnsafeSPSCRingbuffer.Enqueue(q, 999);


            //Buffer is full, can no longer insert.
            Assert.IsFalse(UnsafeSPSCRingbuffer.TryEnqueue(q, 10));

            UnsafeSPSCRingbuffer.Free(q);
        }

        [Test]
        public void TryActionTest()
        {
            var q = UnsafeSPSCRingbuffer.Allocate<int>(16);

            //Inserts 10 items.
            SplitQueue(q);
            var arr = UnsafeSPSCRingbuffer.ToArray<int>(q);
            //Insert 6 more to fill the queue
            for (int i = 0; i < 6; i++)
                UnsafeSPSCRingbuffer.TryEnqueue(q, 999);

            arr = UnsafeSPSCRingbuffer.ToArray<int>(q);
            Assert.IsFalse(UnsafeSPSCRingbuffer.TryEnqueue(q, 10));
            Assert.IsTrue(UnsafeSPSCRingbuffer.TryPeek(q, out int result));
            Assert.AreEqual(0, result);

            for (int i = 0; i < 10; i++)
            {
                Assert.IsTrue(UnsafeSPSCRingbuffer.TryDequeue(q, out int val));
                Assert.AreEqual(i, val);
            }

            //Empty 6 last items
            for (int i = 0; i < 6; i++)
                Assert.IsTrue(UnsafeSPSCRingbuffer.TryDequeue(q, out int val));

            //Empty queue
            Assert.IsFalse(UnsafeSPSCRingbuffer.TryPeek(q, out int res));

            UnsafeSPSCRingbuffer.Free(q);
        }

        [Test]
        public void ClearTest()
        {
            var q = UnsafeSPSCRingbuffer.Allocate<int>(16);

            //Inserts 10 items.
            SplitQueue(q);

            Assert.AreEqual(10, UnsafeSPSCRingbuffer.GetCount(q));
            UnsafeSPSCRingbuffer.Clear(q);
            Assert.AreEqual(0, UnsafeSPSCRingbuffer.GetCount(q));

            Assert.IsTrue(UnsafeSPSCRingbuffer.IsEmpty<int>(q));

            UnsafeSPSCRingbuffer.Free(q);
        }

        [Test]
        public void IteratorTest()
        {
            var q = UnsafeSPSCRingbuffer.Allocate<int>(10);

            //Wrap tail around
            SplitQueue(q);

            //Iterator should start from the head.
            int num = 0;
            foreach (int i in UnsafeSPSCRingbuffer.GetEnumerator<int>(q))
            {
                Assert.AreEqual(num, i);
                num++;
            }

            UnsafeSPSCRingbuffer.Free(q);
        }

#if DEBUG
        [Test]
        public void InvalidTypeTest()
        {
            var q = UnsafeSPSCRingbuffer.Allocate<int>(10);

            Assert.Catch<AssertException>(() => { UnsafeSPSCRingbuffer.Enqueue<float>(q, 162); });

            UnsafeSPSCRingbuffer.Free(q);
        }
#endif

        [Test]
        public void CopyToTest()
        {
            var q = UnsafeSPSCRingbuffer.Allocate<int>(10);
            SplitQueue(q);

            var arr = UnsafeSPSCRingbuffer.ToArray<int>(q);

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(i, arr[i]);
            }
        }

        //[Test]
        public void ConcurrencyTest()
        {
            var q = UnsafeSPSCRingbuffer.Allocate<int>(16);
            int count = 10000;

            Thread reader = new Thread(() =>
            {
                for (int i = 0; i < count; i++)
                {
                    if (i != UnsafeSPSCRingbuffer.Dequeue<int>(q))
                        Assert.Fail("Dequeue gave an unexpected result");
                }
            });

            reader.Start();

            for (int i = 0; i < count; i++)
            {
                UnsafeSPSCRingbuffer.Enqueue(q, i);
            }

            reader.Join();

            UnsafeSPSCRingbuffer.Free(q);
        }
    }
}
