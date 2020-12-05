using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using UnsafeCollections.Collections;

namespace UnsafeCollectionsTests
{
    public unsafe class UnsafeHeapTests
    {
        [Test]
        public void ClearHeapTest()
        {
            var heap = UnsafeHeapMax.Allocate<int, int>(20);
            Assert.AreEqual(0, UnsafeHeapMax.GetCount(heap));

            UnsafeHeapMax.Push(heap, 3, 10);
            UnsafeHeapMax.Push(heap, 1, 1);
            UnsafeHeapMax.Push(heap, 2, 5);

            Assert.AreEqual(3, UnsafeHeapMax.GetCount(heap));

            UnsafeHeapMax.Pop(heap, out int key, out int val);

            Assert.AreEqual(2, UnsafeHeapMax.GetCount(heap));
            Assert.AreEqual(3, key);
            Assert.AreEqual(10, val);

            UnsafeHeapMax.Clear(heap);

            Assert.AreEqual(0, UnsafeHeapMax.GetCount(heap));

            UnsafeHeapMax.Push(heap, 3, 10);
            UnsafeHeapMax.Push(heap, 1, 1);
            UnsafeHeapMax.Push(heap, 2, 5);

            Assert.AreEqual(3, UnsafeHeapMax.GetCount(heap));

            UnsafeHeapMax.Free(heap);
        }
    }
}
