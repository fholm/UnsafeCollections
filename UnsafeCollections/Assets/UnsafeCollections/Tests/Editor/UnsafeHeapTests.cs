using NUnit.Framework;

namespace Collections.Unsafe
{
    public unsafe class UnsafeHeapTests
    {
        [Test]
        public void ClearHeap()
        {
            var heap = UnsafeHeapMax.Allocate<int, int>(20);
            IsTrue(UnsafeHeapMax.Count(heap) == 0);
            UnsafeHeapMax.Push(heap, 3, 10);
            UnsafeHeapMax.Push(heap, 1, 1);
            UnsafeHeapMax.Push(heap, 2, 5);
            IsTrue(UnsafeHeapMax.Count(heap) == 3);
            UnsafeHeapMax.Pop(heap, out int key, out int val);
            IsTrue(UnsafeHeapMax.Count(heap) == 2);
            IsTrue(key == 3);
            IsTrue(val == 10);
            UnsafeHeapMax.Clear(heap);
            IsTrue(UnsafeHeapMax.Count(heap) == 0);
            UnsafeHeapMax.Push(heap, 3, 10);
            UnsafeHeapMax.Push(heap, 1, 1);
            UnsafeHeapMax.Push(heap, 2, 5);
            IsTrue(UnsafeHeapMax.Count(heap) == 3);
        }

        public void IsTrue(bool value)
        {
            NUnit.Framework.Assert.IsTrue(value);
        }
    }
}