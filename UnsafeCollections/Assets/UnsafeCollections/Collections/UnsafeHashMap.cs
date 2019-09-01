/*
The MIT License (MIT)

Copyright (c) 2019 Fredrik Holmstrom

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Collections.Unsafe {
  public unsafe partial struct UnsafeHashMap {


    UnsafeHashCollection _collection;
    int                  _valueOffset;

    public static int Capacity(UnsafeHashMap* map) {
      return map->_collection.Entries.Length;
    }

    public static int Count(UnsafeHashMap* map) {
      return map->_collection.UsedCount - map->_collection.FreeCount;
    }    
    
    public static void Clear(UnsafeHashMap* set)
    {
      UnsafeHashCollection.Clear(&set->_collection);
    }

    public static UnsafeHashMap* Allocate<K, V>(int capacity, bool fixedSize = false)
      where K : unmanaged, IEquatable<K>
      where V : unmanaged {
      return Allocate(capacity, sizeof(K), sizeof(V), fixedSize);
    }

    public static UnsafeHashMap* Allocate(int capacity, int keyStride, int valStride, bool fixedSize = false) {
      var entryStride = sizeof(UnsafeHashCollection.Entry);

      // round capacity up to next prime 
      capacity = UnsafeHashCollection.GetNextPrime(capacity);

      // this has to be true
      Assert.Check(entryStride == 16);

      var keyAlignment = AllocHelper.GetAlignmentForArrayElement(keyStride);
      var valAlignment = AllocHelper.GetAlignmentForArrayElement(valStride);

      // the alignment for entry/key/val, we can't have less than ENTRY_ALIGNMENT
      // bytes alignment because entries are 8 bytes with 2 x 32 bit integers
      var alignment = Math.Max(UnsafeHashCollection.Entry.ALIGNMENT, Math.Max(keyAlignment, valAlignment));

      // calculate strides for all elements
      keyStride   = AllocHelper.RoundUpToAlignment(keyStride,                          alignment);
      valStride   = AllocHelper.RoundUpToAlignment(valStride,                          alignment);
      entryStride = AllocHelper.RoundUpToAlignment(sizeof(UnsafeHashCollection.Entry), alignment);

      // map ptr
      UnsafeHashMap* map;

      if (fixedSize) {
        var sizeOfHeader        = AllocHelper.RoundUpToAlignment(sizeof(UnsafeHashMap),                           alignment);
        var sizeOfBucketsBuffer = AllocHelper.RoundUpToAlignment(sizeof(UnsafeHashCollection.Entry**) * capacity, alignment);
        var sizeofEntriesBuffer = (entryStride + keyStride + valStride) * capacity;

        // allocate memory
        var ptr = AllocHelper.MallocAndClear(sizeOfHeader + sizeOfBucketsBuffer + sizeofEntriesBuffer, alignment);

        // start of memory is the dict itself
        map = (UnsafeHashMap*)ptr;

        // buckets are offset by header size
        map->_collection.Buckets = (UnsafeHashCollection.Entry**)((byte*)ptr + sizeOfHeader);

        // initialize fixed buffer
        UnsafeBuffer.InitFixed(&map->_collection.Entries, (byte*)ptr + (sizeOfHeader + sizeOfBucketsBuffer), capacity, entryStride + keyStride + valStride);
      }
      else {
        // allocate dict, buckets and entries buffer separately
        map                      = AllocHelper.MallocAndClear<UnsafeHashMap>();
        map->_collection.Buckets = (UnsafeHashCollection.Entry**)AllocHelper.MallocAndClear(sizeof(UnsafeHashCollection.Entry**) * capacity, sizeof(UnsafeHashCollection.Entry**));

        // init dynamic buffer
        UnsafeBuffer.InitDynamic(&map->_collection.Entries, capacity, entryStride + keyStride + valStride);
      }

      // header init
      map->_collection.FreeCount = 0;
      map->_collection.UsedCount = 0;
      map->_collection.KeyOffset = entryStride;

      map->_valueOffset = entryStride + keyStride;

      return map;
    }

    public static Iterator<K, V> GetIterator<K, V>(UnsafeHashMap* map)
      where K : unmanaged
      where V : unmanaged {
      return new Iterator<K, V>(map);
    }

    public static bool ContainsKey<K>(UnsafeHashMap* map, K key) where K : unmanaged, IEquatable<K> {
      return UnsafeHashCollection.Find<K>(&map->_collection, key, key.GetHashCode()) != null;
    }

    public static void AddOrGet<K, V>(UnsafeHashMap* map, K key, ref V value)
      where K : unmanaged, IEquatable<K>
      where V : unmanaged {
      var hash  = key.GetHashCode();
      var entry = UnsafeHashCollection.Find<K>(&map->_collection, key, hash);
      if (entry == null) {
        // insert new entry for key
        entry = UnsafeHashCollection.Insert<K>(&map->_collection, key, hash);

        // assign value to entry
        *(V*)GetValue(map, entry) = value;
      }
      else {
        value = *(V*)GetValue(map, entry);
      }
    }

    public static void Add<K, V>(UnsafeHashMap* map, K key, V value)
      where K : unmanaged, IEquatable<K>
      where V : unmanaged {
      var hash  = key.GetHashCode();
      var entry = UnsafeHashCollection.Find<K>(&map->_collection, key, hash);
      if (entry == null) {
        // insert new entry for key
        entry = UnsafeHashCollection.Insert<K>(&map->_collection, key, hash);

        // assign value to entry
        *(V*)GetValue(map, entry) = value;
      }
      else {
        throw new InvalidOperationException();
      }
    }

    public static void Set<K, V>(UnsafeHashMap* map, K key, V value)
      where K : unmanaged, IEquatable<K>
      where V : unmanaged {
      var hash  = key.GetHashCode();
      var entry = UnsafeHashCollection.Find<K>(&map->_collection, key, hash);
      if (entry == null) {
        // insert new entry for key
        entry = UnsafeHashCollection.Insert<K>(&map->_collection, key, hash);
      }

      // assign value to entry
      *(V*)GetValue(map, entry) = value;
    }

    public static V Get<K, V>(UnsafeHashMap* map, K key)
      where K : unmanaged, IEquatable<K>
      where V : unmanaged {
      var entry = UnsafeHashCollection.Find<K>(&map->_collection, key, key.GetHashCode());
      if (entry == null) {
        throw new KeyNotFoundException(key.ToString());
      }

      return *(V*)GetValue(map, entry);
    }

    public static V* GetPtr<K, V>(UnsafeHashMap* map, K key)
      where K : unmanaged, IEquatable<K>
      where V : unmanaged {
      var entry = UnsafeHashCollection.Find<K>(&map->_collection, key, key.GetHashCode());
      if (entry == null) {
        throw new KeyNotFoundException(key.ToString());
      }

      return (V*)GetValue(map, entry);
    }

    public static bool TryGetValue<K, V>(UnsafeHashMap* map, K key, out V val)
      where K : unmanaged, IEquatable<K>
      where V : unmanaged {
      var entry = UnsafeHashCollection.Find<K>(&map->_collection, key, key.GetHashCode());
      if (entry != null) {
        val = *(V*)GetValue(map, entry);
        return true;
      }

      val = default;
      return false;
    }

    public static bool TryGetValuePtr<K, V>(UnsafeHashMap* map, K key, out V* val)
      where K : unmanaged, IEquatable<K>
      where V : unmanaged {
      var entry = UnsafeHashCollection.Find<K>(&map->_collection, key, key.GetHashCode());
      if (entry != null) {
        val = (V*)GetValue(map, entry);
        return true;
      }

      val = null;
      return false;
    }

    public static bool Remove<K>(UnsafeHashMap* map, K key) where K : unmanaged, IEquatable<K> {
      return UnsafeHashCollection.Remove<K>(&map->_collection, key, key.GetHashCode());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void* GetValue(UnsafeHashMap* map, UnsafeHashCollection.Entry* entry) {
      return (byte*)entry + map->_valueOffset;
    }
  }
}