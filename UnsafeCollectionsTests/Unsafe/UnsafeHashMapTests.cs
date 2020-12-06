using NUnit.Framework;
using UnsafeCollections.Collections.Unsafe;

namespace UnsafeCollectionsTests.Unsafe
{
    public unsafe class UnsafeHashMapTests
    {

        private UnsafeHashMap* Map(params int[] values)
        {
            var map = UnsafeHashMap.Allocate<int, int>(values.Length * 2);

            for (int i = 0; i < values.Length; ++i)
            {
                UnsafeHashMap.Add(map, i, values[i]);
            }

            return map;
        }

        [Test]
        public void FreeFixedMap()
        {
            var s = UnsafeHashMap.Allocate<int, int>(2, true);
            UnsafeHashMap.Free(s);
        }

        [Test]
        public void FreeDynamicMap()
        {
            var s = UnsafeHashMap.Allocate<int, int>(2, false);
            UnsafeHashMap.Free(s);
        }

        [Test]
        public void ClearHashMap()
        {
            var map = Map(1, 2, 3);
            Assert.IsTrue(UnsafeHashMap.ContainsKey(map, 2));
            Assert.AreEqual(3, UnsafeHashMap.GetCount(map));
            UnsafeHashMap.TryGetValue(map, 2, out int result);
            Assert.AreEqual(3, result);

            UnsafeHashMap.Add(map, 3, 1);
            Assert.AreEqual(4, UnsafeHashMap.GetCount(map));

            UnsafeHashMap.Clear(map);
            Assert.AreEqual(0, UnsafeHashMap.GetCount(map));
            Assert.IsFalse(UnsafeHashMap.ContainsKey(map, 2));

            UnsafeHashMap.Add(map, 3, 10);
            Assert.AreEqual(1, UnsafeHashMap.GetCount(map));
            Assert.IsTrue(UnsafeHashMap.ContainsKey(map, 3));
            UnsafeHashMap.TryGetValue(map, 3, out int result2);
            Assert.AreEqual(10, result2);

            UnsafeHashMap.Clear(map);
            Assert.AreEqual(0, UnsafeHashMap.GetCount(map));

            UnsafeHashMap.Free(map);
        }
    }
}
