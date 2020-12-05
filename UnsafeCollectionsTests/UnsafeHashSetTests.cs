using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using UnsafeCollections.Collections;

namespace UnsafeCollectionsTests
{
    public unsafe class UnsafeHashSetTests
    {
        private UnsafeHashSet* Set(params int[] values)
        {
            var set = UnsafeHashSet.Allocate<int>(values.Length * 2);

            for (int i = 0; i < values.Length; ++i)
            {
                UnsafeHashSet.Add(set, values[i]);
            }

            return set;
        }

        [Test]
        public void FreeFixedSet()
        {
            var s = UnsafeHashSet.Allocate<int>(2, true);
            UnsafeHashSet.Free(s);
        }

        [Test]
        public void FreeDynamicSet()
        {
            var s = UnsafeHashSet.Allocate<int>(2, false);
            UnsafeHashSet.Free(s);
        }

        [Test]
        public void ClearHashSet()
        {
            var set = Set(1, 2, 3);
            Assert.IsTrue(UnsafeHashSet.Contains(set, 2));
            Assert.AreEqual(3, UnsafeHashSet.GetCount(set));

            UnsafeHashSet.Add(set, 4);
            Assert.AreEqual(4, UnsafeHashSet.GetCount(set));

            UnsafeHashSet.Clear(set);
            Assert.AreEqual(0, UnsafeHashSet.GetCount(set));
            Assert.IsFalse(UnsafeHashSet.Contains(set, 2));

            UnsafeHashSet.Add(set, 4);
            Assert.AreEqual(1, UnsafeHashSet.GetCount(set));
            Assert.IsTrue(UnsafeHashSet.Contains(set, 4));

            UnsafeHashSet.Clear(set);
            Assert.AreEqual(0, UnsafeHashSet.GetCount(set));

            UnsafeHashSet.Free(set);
        }
    }
}
