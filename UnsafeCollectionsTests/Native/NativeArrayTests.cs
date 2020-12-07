using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using UnsafeCollections.Collections.Native;
using UnsafeCollections.Debug.TypeProxies;

namespace UnsafeCollectionsTests.Native
{
    public unsafe class NativeArrayTests
    {
        //[Test]
        public void Test()
        {
            NativeArray<float> arr = new NativeArray<float>(10);

            for (int i = 0; i < 10; i++)
            {
                arr[i] = (float)(i * i) / 3;
            }

        }

        [Test]
        public void ConstructorTest()
        {
            NativeArray<int> arr = new NativeArray<int>(10);

            Assert.IsTrue(arr.IsCreated);

            arr.Dispose();

            Assert.IsFalse(arr.IsCreated);
        }

        [Test]
        public void IndexTest()
        {
            NativeArray<int> arr = new NativeArray<int>(10);

            for (int i = 0; i < 10; i++)
                arr[i] = i;

            for (int i = 0; i < 10; i++)
                Assert.AreEqual(i, arr[i]);

            arr.Dispose();
        }

        [Test]
        public void IteratorTest()
        {
            NativeArray<int> arr = new NativeArray<int>(10);

            for (int i = 0; i < 10; i++)
                arr[i] = i;

            int c = 0;
            foreach (int i in arr.NativeIterator)
                Assert.AreEqual(c++, i);

            arr.Dispose();
        }

        [Test]
        public void FromArrayTest()
        {
            var intArr = new int[10];

            for (int i = 0; i < 10; i++)
                intArr[i] = i;

            NativeArray<int> arr = new NativeArray<int>(intArr);

            Assert.AreEqual(intArr.Length, arr.Length);

            for (int i = 0; i < 10; i++)
                Assert.AreEqual(i, arr[i]);

            arr.Dispose();
        }

        [Test]
        public void ToArrayTest()
        {
            NativeArray<int> arr = new NativeArray<int>(10);

            for (int i = 0; i < 10; i++)
                arr[i] = i;

            var intArr = arr.ToArray();

            for (int i = 0; i < 10; i++)
                Assert.AreEqual(i, intArr[i]);

            arr.Dispose();
        }

        public void ContainsTest()
        {
            NativeArray<int> arr = new NativeArray<int>(10);

            for (int i = 0; i < 10; i++)
                arr[i] = i;

            arr.Dispose();
        }
    }
}
