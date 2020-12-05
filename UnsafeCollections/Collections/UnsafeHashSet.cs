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
using System.Collections;
using System.Collections.Generic;
using UnsafeCollections.Unsafe;

namespace UnsafeCollections.Collections
{
    public unsafe struct UnsafeHashSet
    {
        UnsafeHashCollection _collection;

        public static UnsafeHashSet* Allocate<T>(int capacity, bool fixedSize = false)
          where T : unmanaged, IEquatable<T>
        {
            return Allocate(capacity, sizeof(T), fixedSize);
        }

        public static UnsafeHashSet* Allocate(int capacity, int valStride, bool fixedSize = false)
        {
            var entryStride = sizeof(UnsafeHashCollection.Entry);

            // round capacity up to next prime 
            capacity = UnsafeHashCollection.GetNextPrime(capacity);

            // this has to be true
            UDebug.Assert(entryStride == 16);

            var valAlignment = Memory.GetAlignment(valStride);

            // the alignment for entry/key/val, we can't have less than ENTRY_ALIGNMENT
            // bytes alignment because entries are 16 bytes with 1 x pointer + 2 x 4 byte integers
            var alignment = Math.Max(UnsafeHashCollection.Entry.ALIGNMENT, valAlignment);

            // calculate strides for all elements
            valStride = Memory.RoundToAlignment(valStride, alignment);
            entryStride = Memory.RoundToAlignment(sizeof(UnsafeHashCollection.Entry), alignment);

            // dictionary ptr
            UnsafeHashSet* set;

            if (fixedSize)
            {
                var sizeOfHeader = Memory.RoundToAlignment(sizeof(UnsafeHashSet), alignment);
                var sizeOfBucketsBuffer = Memory.RoundToAlignment(sizeof(UnsafeHashCollection.Entry**) * capacity, alignment);
                var sizeofEntriesBuffer = (entryStride + valStride) * capacity;

                // allocate memory
                var ptr = Memory.MallocAndZero(sizeOfHeader + sizeOfBucketsBuffer + sizeofEntriesBuffer, alignment);

                // start of memory is the dict itself
                set = (UnsafeHashSet*)ptr;

                // buckets are offset by header size
                set->_collection.Buckets = (UnsafeHashCollection.Entry**)((byte*)ptr + sizeOfHeader);

                // initialize fixed buffer
                UnsafeBuffer.InitFixed(&set->_collection.Entries, (byte*)ptr + (sizeOfHeader + sizeOfBucketsBuffer), capacity, entryStride + valStride);
            }
            else
            {
                // allocate dict, buckets and entries buffer separately
                set = Memory.MallocAndZero<UnsafeHashSet>();
                set->_collection.Buckets = (UnsafeHashCollection.Entry**)Memory.MallocAndZero(sizeof(UnsafeHashCollection.Entry**) * capacity, sizeof(UnsafeHashCollection.Entry**));

                // init dynamic buffer
                UnsafeBuffer.InitDynamic(&set->_collection.Entries, capacity, entryStride + valStride);
            }

            set->_collection.FreeCount = 0;
            set->_collection.UsedCount = 0;
            set->_collection.KeyOffset = entryStride;

            return set;
        }

        public static void Free(UnsafeHashSet* set)
        {
            if (set == null)
                return;

            if (set->_collection.Entries.Dynamic == 1)
            {
                UnsafeHashCollection.Free(&set->_collection);
            }

            *set = default;

            Memory.Free(set);
        }

        public static int GetCapacity(UnsafeHashSet* set)
        {
            return set->_collection.Entries.Length;
        }

        public static int GetCount(UnsafeHashSet* set)
        {
            return set->_collection.UsedCount - set->_collection.FreeCount;
        }

        public static void Clear(UnsafeHashSet* set)
        {
            UnsafeHashCollection.Clear(&set->_collection);
        }

        public static bool Add<T>(UnsafeHashSet* set, T key)
          where T : unmanaged, IEquatable<T>
        {
            var hash = key.GetHashCode();
            var entry = UnsafeHashCollection.Find<T>(&set->_collection, key, hash);
            if (entry == null)
            {
                UnsafeHashCollection.Insert<T>(&set->_collection, key, hash);
                return true;
            }

            return false;
        }

        public static bool Remove<T>(UnsafeHashSet* set, T key) where T : unmanaged, IEquatable<T>
        {
            return UnsafeHashCollection.Remove<T>(&set->_collection, key, key.GetHashCode());
        }

        public static bool Contains<T>(UnsafeHashSet* set, T key) where T : unmanaged, IEquatable<T>
        {
            return UnsafeHashCollection.Find<T>(&set->_collection, key, key.GetHashCode()) != null;
        }

        public static HashSetIterator<T> GetIterator<T>(UnsafeHashSet* set) where T : unmanaged
        {
            return new HashSetIterator<T>(set);
        }

        public static void And<T>(UnsafeHashSet* set, UnsafeHashSet* other) where T : unmanaged, IEquatable<T>
        {
            for (int i = set->_collection.UsedCount - 1; i >= 0; --i)
            {
                var entry = UnsafeHashCollection.GetEntry(&set->_collection, i);
                if (entry->State == UnsafeHashCollection.EntryState.Used)
                {
                    var key = *(T*)((byte*)entry + set->_collection.KeyOffset);
                    var keyHash = key.GetHashCode();

                    // if we don't find this in other collection, remove it (And)
                    if (UnsafeHashCollection.Find<T>(&other->_collection, key, keyHash) == null)
                    {
                        UnsafeHashCollection.Remove<T>(&set->_collection, key, keyHash);
                    }
                }
            }
        }

        public static void Or<T>(UnsafeHashSet* set, UnsafeHashSet* other) where T : unmanaged, IEquatable<T>
        {
            for (int i = other->_collection.UsedCount - 1; i >= 0; --i)
            {
                var entry = UnsafeHashCollection.GetEntry(&other->_collection, i);
                if (entry->State == UnsafeHashCollection.EntryState.Used)
                {
                    // always add to this collection
                    Add<T>(set, *(T*)((byte*)entry + other->_collection.KeyOffset));
                }
            }
        }

        public static void Xor<T>(UnsafeHashSet* set, UnsafeHashSet* other) where T : unmanaged, IEquatable<T>
        {
            for (int i = other->_collection.UsedCount - 1; i >= 0; --i)
            {
                var entry = UnsafeHashCollection.GetEntry(&other->_collection, i);
                if (entry->State == UnsafeHashCollection.EntryState.Used)
                {
                    var key = *(T*)((byte*)entry + other->_collection.KeyOffset);
                    var keyHash = key.GetHashCode();

                    // if we don't find it in our collection, add it
                    if (UnsafeHashCollection.Find<T>(&set->_collection, key, keyHash) == null)
                    {
                        UnsafeHashCollection.Insert<T>(&set->_collection, key, keyHash);
                    }

                    // if we do, remove it
                    else
                    {
                        UnsafeHashCollection.Remove<T>(&set->_collection, key, keyHash);
                    }
                }
            }
        }


        public unsafe struct HashSetIterator<T> : IUnsafeIterator<T> where T : unmanaged
        {
            UnsafeHashCollection.Iterator _iterator;
            readonly int _keyOffset;

            public HashSetIterator(UnsafeHashSet* set)
            {
                _keyOffset = set->_collection.KeyOffset;
                _iterator = new UnsafeHashCollection.Iterator(&set->_collection);
            }

            public bool MoveNext()
            {
                return _iterator.Next();
            }

            public void Reset()
            {
                _iterator.Reset();
            }

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public T Current
            {
                get
                {
                    if (_iterator.Current == null)
                    {
                        throw new InvalidOperationException();
                    }

                    return *(T*)((byte*)_iterator.Current + _keyOffset);
                }
            }

            public void Dispose()
            {
            }

            public IEnumerator<T> GetEnumerator()
            {
                return this;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
    }
}