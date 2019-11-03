using NUnit.Framework;

namespace Collections.Unsafe
{
    public unsafe class UnsafeBitSetTests
    {
        [Test]
        public void TestBitSet()
        {
            UnsafeBitSet* bitSet = UnsafeBitSet.Alloc(64);

            UnsafeBitSet.Set(bitSet, 1);
            UnsafeBitSet.Set(bitSet, 2);
            UnsafeBitSet.Set(bitSet, 3);
            UnsafeBitSet.Set(bitSet, 61);

            UnsafeArray* setBits = UnsafeArray.Allocate<int>(UnsafeBitSet.Size(bitSet));

            var setBitsCount = UnsafeBitSet.GetSetBits(bitSet, setBits);

            for (int i = 0; i < setBitsCount; i++)
            {
                IsTrue(UnsafeBitSet.IsSet(bitSet, UnsafeArray.Get<int>(setBits, i)));
            }
        }

        public void IsTrue(bool value)
        {
            NUnit.Framework.Assert.IsTrue(value);
        }
    }
}