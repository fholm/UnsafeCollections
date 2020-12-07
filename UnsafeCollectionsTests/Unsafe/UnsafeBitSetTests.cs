using NUnit.Framework;
using UnsafeCollections.Collections.Unsafe;

namespace UnsafeCollectionsTests.Unsafe
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

        [Test]
        public void BitSetIteratorTest()
        {
            UnsafeBitSet* bitSet = UnsafeBitSet.Allocate(8);

            for (int i = 0; i < 8; i++)
            {
                if (i % 2 == 0)
                    UnsafeBitSet.Set(bitSet, i);
            }

            int index = 0;
            foreach (var set in UnsafeBitSet.GetEnumerator(bitSet))
            {
                if (index % 2 == 0)
                    Assert.IsTrue(set.set);

                index++;
            }

            UnsafeBitSet.Free(bitSet);
        }
    }
}
