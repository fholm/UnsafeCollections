using NUnit.Framework;
using UnsafeCollections;
using UnsafeCollections.Collections.Unsafe;

namespace UnsafeCollectionsTests.Unsafe
{
    public unsafe class UnsafeArrayTests
    {
        [Test]
        public void ConstructorTest()
        {
            var arr = UnsafeArray.Allocate<int>(10);

            Assert.AreEqual(UnsafeArray.GetLength(arr), 10);
            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(0, UnsafeArray.Get<int>(arr, i));
            }

            UnsafeArray.Free(arr);
        }

        [Test]
        public void MutateTest()
        {
            var arr = UnsafeArray.Allocate<int>(10);

            for (int i = 0; i < 10; i++)
            {
                UnsafeArray.Set(arr, i, i);
            }

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(i, UnsafeArray.Get<int>(arr, i));
            }

            UnsafeArray.Free(arr);
        }

#if DEBUG
        [Test]
        public void InvalidTypeTest()
        {
            var arr = UnsafeArray.Allocate<int>(10);

            Assert.Catch<AssertException>(() => { UnsafeArray.Set<float>(arr, 4, 20); });

            UnsafeArray.Free(arr);
        }
#endif

        [Test]
        public void IteratorTest()
        {
            var arr = UnsafeArray.Allocate<int>(10);

            unsafe
            {
                var itr = UnsafeArray.GetIterator<int>(arr);
                for (int i = 0; i < 10; i++)
                    UnsafeArray.Set(arr, i, i * i);

                int num = 0;
                foreach (int i in itr)
                {
                    Assert.AreEqual(num * num, i);
                    num++;
                }
            }

            UnsafeArray.Free(arr);
        }
    }
}
