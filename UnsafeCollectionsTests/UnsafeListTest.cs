using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using UnsafeCollections;
using UnsafeCollections.Collections;

namespace UnsafeCollectionsTests
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

            unsafe
            {
                var itr = UnsafeList.GetIterator<int>(arr);
                for (int i = 0; i < 10; i++)
                    UnsafeList.Add(arr, i * i);

                int num = 0;
                foreach (int i in itr)
                {
                    Assert.AreEqual(num * num, i);
                    num++;
                }
            }

            UnsafeList.Free(arr);
        }
    }
}
