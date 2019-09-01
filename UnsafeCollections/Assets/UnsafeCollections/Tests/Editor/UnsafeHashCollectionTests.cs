using NUnit.Framework;

namespace Collections.Unsafe
{
    public unsafe class UnsafeHashCollectionTests
    {
        static UnsafeHashSet* Set(params int[] values)
        {
            var c = UnsafeHashSet.Allocate<int>(values.Length * 2);
            
            for (int i = 0; i < values.Length; ++i)
            {
                UnsafeHashSet.Add(c, values[i]);
            }

            return c;
        }

        [Test]
        public void ClearHashSet()
        {
            var set = Set(1, 2, 3);
            IsTrue(UnsafeHashSet.Contains(set, 2));
            IsTrue(UnsafeHashSet.Count(set)==3);
            
            UnsafeHashSet.Add(set, 4);
            IsTrue(UnsafeHashSet.Count(set)==4);
            
            UnsafeHashSet.Clear(set);
            IsTrue(UnsafeHashSet.Count(set)==0);
            IsTrue(!UnsafeHashSet.Contains(set, 2));
            
            UnsafeHashSet.Add(set, 4);
            IsTrue(UnsafeHashSet.Count(set)==1);
            IsTrue(UnsafeHashSet.Contains(set, 4));
            
            UnsafeHashSet.Clear(set);
            IsTrue(UnsafeHashSet.Count(set) == 0);
        }
        
        static UnsafeHashMap* Map(params int[] values)
        {
            var c = UnsafeHashMap.Allocate<int, int>(values.Length * 2);
            
            for (int i = 0; i < values.Length; ++i)
            {
                UnsafeHashMap.Add(c, i, values[i]);
            }

            return c;
        }

        [Test]
        public void ClearHashMap()
        {
            var set = Map(1, 2, 3);
            IsTrue(UnsafeHashMap.ContainsKey(set, 2));
            IsTrue(UnsafeHashMap.Count(set)==3);
            UnsafeHashMap.TryGetValue(set, 2, out int result);
            IsTrue(result == 3);
            
            UnsafeHashMap.Add(set, 3, 1);
            IsTrue(UnsafeHashMap.Count(set)==4);
            
            UnsafeHashMap.Clear(set);
            IsTrue(UnsafeHashMap.Count(set)==0);
            IsTrue(!UnsafeHashMap.ContainsKey(set, 2));
            
            UnsafeHashMap.Add(set,3, 10);
            IsTrue(UnsafeHashMap.Count(set)==1);
            IsTrue(UnsafeHashMap.ContainsKey(set, 3));
            UnsafeHashMap.TryGetValue(set, 3, out int result2);
            IsTrue(result2 == 10);

            UnsafeHashMap.Clear(set);
            IsTrue(UnsafeHashMap.Count(set) == 0);
        }

        public void IsTrue(bool value)
        {
            NUnit.Framework.Assert.IsTrue(value);
        }
    }
}