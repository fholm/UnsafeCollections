using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using UnsafeCollections.Collections;

namespace UnsafeCollectionsTests
{
    public unsafe class UnsafeBitSetTests
    {
        [Test]
        public void TestBitSet()
        {
            UnsafeBitSet* bitSet = UnsafeBitSet.Allocate(64);

            UnsafeBitSet.Set(bitSet, 1);
            UnsafeBitSet.Set(bitSet, 2);
            UnsafeBitSet.Set(bitSet, 3);
            UnsafeBitSet.Set(bitSet, 61);

            UnsafeArray* setBits = UnsafeArray.Allocate<int>(UnsafeBitSet.GetSize(bitSet));

            var setBitsCount = UnsafeBitSet.GetSetBits(bitSet, setBits);

            for (int i = 0; i < setBitsCount; i++)
            {
                Assert.IsTrue(UnsafeBitSet.IsSet(bitSet, UnsafeArray.Get<int>(setBits, i)));
            }

            UnsafeBitSet.Free(bitSet);
            UnsafeArray.Free(setBits);
        }
    }
}
