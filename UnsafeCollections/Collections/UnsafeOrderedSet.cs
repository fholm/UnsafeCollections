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
    public unsafe struct UnsafeOrderedSet
    {
        UnsafeOrderedCollection _collection;

        public static UnsafeOrderedSet* Allocate<T>(int capacity, bool fixedSize = false)
          where T : unmanaged, IComparable<T>
        {
            return Allocate(capacity, sizeof(T), fixedSize);
        }

        public static UnsafeOrderedSet* Allocate(int capacity, int valStride, bool fixedSize = false)
        {
            var entryStride = sizeof(UnsafeOrderedCollection.Entry);
            var valAlignment = Memory.GetAlignment(valStride);

            // the alignment for entry/key/val, we can't have less than ENTRY_ALIGNMENT
            // bytes alignment because entries are 12 bytes with 3 x 32 bit integers
            var alignment = Math.Max(UnsafeOrderedCollection.Entry.ALIGNMENT, valAlignment);

            // calculate strides for all elements
            valStride = Memory.RoundToAlignment(valStride, alignment);
            entryStride = Memory.RoundToAlignment(entryStride, alignment);

            // dictionary ptr
            UnsafeOrderedSet* set;

            if (fixedSize)
            {
                var sizeOfHeader = Memory.RoundToAlignment(sizeof(UnsafeOrderedSet), alignment);
                var sizeofEntriesBuffer = (entryStride + valStride) * capacity;

                // allocate memory
                var ptr = Memory.MallocAndZero(sizeOfHeader + sizeofEntriesBuffer, alignment);

                // start of memory is the set itself
                set = (UnsafeOrderedSet*)ptr;

                // initialize fixed buffer
                UnsafeBuffer.InitFixed(&set->_collection.Entries, (byte*)ptr + sizeOfHeader, capacity, entryStride + valStride);
            }
            else
            {
                // allocate set separately
                set = Memory.MallocAndZero<UnsafeOrderedSet>();

                // init dynamic buffer
                UnsafeBuffer.InitDynamic(&set->_collection.Entries, capacity, entryStride + valStride);
            }

            set->_collection.FreeCount = 0;
            set->_collection.UsedCount = 0;
            set->_collection.KeyOffset = entryStride;

            return set;
        }

        public static void Free(UnsafeOrderedSet* set)
        {
            if (set == null)
                return;

            if (set->_collection.Entries.Dynamic == 1)
            {
                UnsafeBuffer.Free(&set->_collection.Entries);
            }

            // clear memory
            *set = default;

            // free it
            Memory.Free(set);
        }

        public static OrderedSetIterator<T> GetIterator<T>(UnsafeOrderedSet* set) where T : unmanaged
        {
            return new OrderedSetIterator<T>(set);
        }

        public static int GetCount(UnsafeOrderedSet* set)
        {
            return UnsafeOrderedCollection.GetCount(&set->_collection);
        }

        public static void Add<T>(UnsafeOrderedSet* set, T item)
          where T : unmanaged, IComparable<T>
        {
            UnsafeOrderedCollection.Insert<T>(&set->_collection, item);
        }

        public static void Remove<T>(UnsafeOrderedSet* set, T item)
          where T : unmanaged, IComparable<T>
        {
            UnsafeOrderedCollection.Remove<T>(&set->_collection, item);
        }

        public static bool Contains<T>(UnsafeOrderedSet* set, T item)
          where T : unmanaged, IComparable<T>
        {
            return UnsafeOrderedCollection.Find<T>(&set->_collection, item) != null;
        }


        public unsafe struct OrderedSetIterator<T> : IUnsafeIterator<T> where T : unmanaged
        {
            readonly int _keyOffset;
            UnsafeOrderedCollection.OrderedCollectionIterator _iterator;

            public OrderedSetIterator(UnsafeOrderedSet* set)
            {
                _keyOffset = set->_collection.KeyOffset;
                _iterator = new UnsafeOrderedCollection.OrderedCollectionIterator(&set->_collection);
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

            object IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                return _iterator.Next();
            }

            public void Reset()
            {
                _iterator.Reset();
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