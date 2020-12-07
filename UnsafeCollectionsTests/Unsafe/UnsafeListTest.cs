using NUnit.Framework;
using UnsafeCollections;
using UnsafeCollections.Collections.Unsafe;

namespace UnsafeCollectionsTests.Unsafe
{
    public unsafe class UnsafeListTest
    {
        [Test]
        public void ConstructorTest()
        {
            var arr = UnsafeList.Allocate<int>(10);

            Assert.AreEqual(UnsafeList.GetCount(arr), 0);

            UnsafeList.Free(arr);
        }

        [Test]
        public void MutateTest()
        {
            var arr = UnsafeList.Allocate<int>(10);

            for (int i = 0; i < 10; i++)
            {
                UnsafeList.Add(arr, i);
            }

            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(i, UnsafeList.Get<int>(arr, i));
            }

            UnsafeList.Free(arr);
        }

#if DEBUG
        [Test]
        public void InvalidTypeTest()
        {
            var arr = UnsafeList.Allocate<int>(10);

            Assert.Catch<AssertException>(() => { UnsafeList.Set<float>(arr, 4, 20); });

            UnsafeList.Free(arr);
        }
#endif

        [Test]
        public void IteratorTest()
        {
            var arr = UnsafeList.Allocate<int>(10);

            var itr = UnsafeList.GetEnumerator<int>(arr);
            for (int i = 0; i < 10; i++)
                UnsafeList.Add(arr, i * i);

            Assert.AreEqual(10, UnsafeList.GetCount(arr));

            int num = 0;
            foreach (int i in itr)
            {
                Assert.AreEqual(num * num, i);
                num++;
            }

            UnsafeList.Free(arr);
        }

        [Test]
        public void RemoveTest()
        {
            var arr = UnsafeList.Allocate<int>(10);
            for (int i = 1; i <= 10; i++)
                UnsafeList.Add(arr, i);

            Assert.AreEqual(10, UnsafeList.GetCount(arr));

            UnsafeList.RemoveAt(arr, 4); //Remove number 5
            Assert.AreEqual(9, UnsafeList.GetCount(arr));

            int offs = 0;
            for (int i = 1; i < 10; i++)
            {
                if (i == 5) offs++; //Skip previously removed 5
                var num = UnsafeList.Get<int>(arr, i - 1);
                Assert.AreEqual(i + offs, num);
            }
        }

        [Test]
        public void IndexOfTest()
        {
            var arr = UnsafeList.Allocate<int>(10);
            for (int i = 1; i <= 10; i++)
                UnsafeList.Add(arr, i);

            var index = UnsafeList.IndexOf(arr, 5);
            Assert.AreEqual(4, index);
        }
    }
}
