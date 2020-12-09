using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnsafeCollections;
using UnsafeCollections.Collections.Unsafe;

namespace UnsafeCollectionsTests.Unsafe
{
    public unsafe class UnsafeQueueTests
    {
        private static void SplitQueue(UnsafeQueue* q)
        {
            //Wrap tail back to 0
            for (int i = 0; i < 5; i++)
                UnsafeQueue.Enqueue(q, 111);

            //First half
            for (int i = 0; i < 5; i++)
                UnsafeQueue.Enqueue(q, i);

            //Move head by 5
            for (int i = 0; i < 5; i++)
                UnsafeQueue.Dequeue<int>(q);

            //Second half (head and tail are now both 5)
            for (int i = 5; i < 10; i++)
                UnsafeQueue.Enqueue(q, i);

            //Circular buffer now "ends" in the middle of the underlying array
        }


        [Test]
        public void ConstructorTest()
        {
            var q = UnsafeQueue.Allocate<int>(10);

            Assert.AreEqual(0, UnsafeQueue.GetCount(q));
            Assert.AreEqual(10, UnsafeQueue.GetCapacity(q));
            Assert.IsFalse(UnsafeQueue.IsFixedSize(q));

            UnsafeQueue.Free(q);
        }

        [Test]
        public void EnqueueTest()
        {
            var q = UnsafeQueue.Allocate<int>(10);

            for (int i = 0; i < 10; i++)
            {
                UnsafeQueue.Enqueue(q, i * i);
            }

            Assert.AreEqual(10, UnsafeQueue.GetCount(q));
            Assert.AreEqual(10, UnsafeQueue.GetCapacity(q));

            UnsafeQueue.Clear(q);

            Assert.AreEqual(0, UnsafeQueue.GetCount(q));
            Assert.AreEqual(10, UnsafeQueue.GetCapacity(q));

            UnsafeQueue.Free(q);
        }

        [Test]
        public void DequeueTest()
        {
            var q = UnsafeQueue.Allocate<int>(10);

            for (int i = 0; i < 10; i++)
                UnsafeQueue.Enqueue(q, i * i);


            for (int i = 0; i < 10; i++)
            {
                int num = UnsafeQueue.Dequeue<int>(q);
                Assert.AreEqual(i * i, num);
            }

            UnsafeQueue.Free(q);
        }

        [Test]
        public void PeekTest()
        {
            var q = UnsafeQueue.Allocate<int>(10);

            for (int i = 0; i < 10; i++)
                UnsafeQueue.Enqueue(q, (int)Math.Pow(i + 2, 2));

            for (int i = 0; i < 10; i++)
            {
                int num = UnsafeQueue.Peek<int>(q);
                Assert.AreEqual(4, num);
            }

            //Verify no items are dequeued
            Assert.AreEqual(10, UnsafeQueue.GetCount(q));

            UnsafeQueue.Free(q);
        }

        [Test]
        public void ExpandTest()
        {
            throw new NotImplementedException();
        }

        [Test]
        public void TryActionTest()
        {
            throw new NotImplementedException();
        }

        [Test]
        public void IteratorTest()
        {
            var q = UnsafeQueue.Allocate<int>(10);

            //Wrap tail around
            SplitQueue(q);

            //Iterator should start from the head.
            int num = 0;
            foreach (int i in UnsafeQueue.GetEnumerator<int>(q))
            {
                Assert.AreEqual(num, i);
                num++;
            }

            UnsafeQueue.Free(q);
        }

        [Test]
        public void Contains()
        {
            var q = UnsafeQueue.Allocate<int>(10);

            //Wrap tail around
            SplitQueue(q);


            //Check tail and head end of the queue
            Assert.IsTrue(UnsafeQueue.Contains(q, 1));
            Assert.IsTrue(UnsafeQueue.Contains(q, 9));
            Assert.False(UnsafeQueue.Contains(q, 11));

            UnsafeQueue.Free(q);
        }

#if DEBUG
        [Test]
        public void InvalidTypeTest()
        {
            var q = UnsafeQueue.Allocate<int>(10);

            Assert.Catch<AssertException>(() => { UnsafeQueue.Enqueue<float>(q, 162); });

            UnsafeQueue.Free(q);
        }
#endif

        [Test]
        public void CopyToTest()
        {
            var q = UnsafeQueue.Allocate<int>(10);
            SplitQueue(q);

            var arr = new int[10];
            fixed (void* ptr = arr)
            {
                UnsafeQueue.CopyTo<int>(q, ptr, 0);
            }

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(i, arr[i]);
            }
        }
    }
}
