/*
The MIT License (MIT)

Copyright (c) 2019 Fredrik Holmstrom
Copyright (c) 2020 Dennis Corvers

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
using System.Runtime.CompilerServices;
using UnsafeCollections.Unsafe;

namespace UnsafeCollections.Collections.Unsafe
{
    public unsafe struct UnsafeList
    {
        const string LIST_FULL = "Fixed size list is full";
        const string LIST_FIXED_CANT_CHANGE_CAPACITY = "Fixed size list can't change its capacity";
        const string LIST_INIT_TOO_SMALL = "Pointer length for must be large enough to contain both header and at least 1 item";

        UnsafeBuffer _items;
        int _count;
        IntPtr _typeHandle;

        public static UnsafeList* Allocate<T>(int capacity, bool fixedSize = false) where T : unmanaged
        {
            UDebug.Assert(capacity > 0);
            int stride = sizeof(T);


            UnsafeList* list;

            // fixedSize means we are allocating the memory for the collection header and the items in it as one block
            if (fixedSize)
            {
                var alignment = Memory.GetAlignment(stride);

                // align header size to the elements alignment
                var sizeOfHeader = Memory.RoundToAlignment(sizeof(UnsafeList), alignment);
                var sizeOfBuffer = stride * capacity;

                // allocate memory for list and array with the correct alignment
                var ptr = Memory.MallocAndZero(sizeOfHeader + sizeOfBuffer);

                // grab header ptr
                list = (UnsafeList*)ptr;

                // initialize fixed buffer from same block of memory as the collection, offset by sizeOfHeader
                UnsafeBuffer.InitFixed(&list->_items, (byte*)ptr + sizeOfHeader, capacity, stride);
            }
            else
            {
                // allocate collection separately
                list = Memory.MallocAndZero<UnsafeList>();

                // initialize dynamic buffer with separate memory
                UnsafeBuffer.InitDynamic(&list->_items, capacity, stride);
            }

            list->_count = 0;
            list->_typeHandle = typeof(T).TypeHandle.Value;
            return list;
        }

        public static void Free(UnsafeList* list)
        {
            if (list == null)
                return;

            // free dynamic items separately
            if (list->_items.Dynamic == 1)
            {
                UnsafeBuffer.Free(&list->_items);
            }

            // clear memory
            *list = default;

            // free list
            Memory.Free(list);
        }

        public static int GetCount(UnsafeList* list)
        {
            UDebug.Assert(list != null);
            UDebug.Assert(list->_items.Ptr != null);
            return list->_count;
        }

        public static void Clear(UnsafeList* list)
        {
            UDebug.Assert(list != null);
            UDebug.Assert(list->_items.Ptr != null);
            list->_count = 0;
        }

        public static int GetCapacity(UnsafeList* list)
        {
            UDebug.Assert(list != null);
            UDebug.Assert(list->_items.Ptr != null);
            return list->_items.Length;
        }

        public static bool IsFixedSize(UnsafeList* list)
        {
            UDebug.Assert(list != null);
            UDebug.Assert(list->_items.Ptr != null);
            return list->_items.Dynamic == 0;
        }

        public static void SetCapacity(UnsafeList* list, int capacity)
        {
            UDebug.Assert(list != null);
            UDebug.Assert(list->_items.Ptr != null);

            if (list->_items.Dynamic == 0)
            {
                throw new InvalidOperationException(LIST_FIXED_CANT_CHANGE_CAPACITY);
            }

            // no change in capacity
            if (capacity == list->_items.Length)
            {
                return;
            }

            // tried to set to zero or negative, so free items
            if (capacity <= 0)
            {
                // have to make sure to set count to 0
                list->_count = 0;

                // and clear memory for items
                if (list->_items.Ptr != null)
                {
                    UnsafeBuffer.Free(&list->_items);
                }

                return;
            }

            // allocate new items 
            UnsafeBuffer newItems = default;
            UnsafeBuffer.InitDynamic(&newItems, capacity, list->_items.Stride);

            // if have anything in list, copy it 
            if (list->_count > 0)
            {
                // also make sure that count is
                // not larger than the new capacity
                if (list->_count > capacity)
                {
                    list->_count = capacity;
                }

                // copy over elements
                UnsafeBuffer.Copy(list->_items, 0, newItems, 0, list->_count);
            }

            // if an existing buffer was here, free it 
            if (list->_items.Ptr != null)
            {
                UnsafeBuffer.Free(&list->_items);
            }

            // assign new buffer
            list->_items = newItems;
        }

        public static void Add<T>(UnsafeList* list, T item) where T : unmanaged
        {
            UDebug.Assert(list != null);
            UDebug.Assert(list->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == list->_typeHandle);

            var count = list->_count;
            var items = list->_items;

            // fast path
            if (count < items.Length)
            {
                // set element 
                *items.Element<T>(count) = item;

                // increment count
                list->_count = count + 1;

                return;
            }

            if (list->_items.Dynamic == 0)
            {
                throw new InvalidOperationException(LIST_FULL);
            }

            // double capacity, make sure that if length is 0 then we set capacity to at least 2
            SetCapacity(list, Math.Max(2, items.Length * 2));

            // re-assign items after expand
            items = list->_items;

            // this has to hold now
            UDebug.Assert(count < items.Length);

            // set element 
            *items.Element<T>(count) = item;

            // increment count
            list->_count = count + 1;
        }

        public static void Set<T>(UnsafeList* list, int index, T item) where T : unmanaged
        {
            UDebug.Assert(list != null);
            UDebug.Assert(list->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == list->_typeHandle);

            // cast to uint trick, which eliminates < 0 check
            if ((uint)index >= (uint)list->_count)
            {
                throw new IndexOutOfRangeException();
            }

            var items = list->_items;
            *items.Element<T>(index) = item;
        }

        public static T Get<T>(UnsafeList* list, int index) where T : unmanaged
        {
            return *GetPtr<T>(list, index);
        }

        public static T* GetPtr<T>(UnsafeList* list, int index) where T : unmanaged
        {
            UDebug.Assert(list != null);
            UDebug.Assert(list->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == list->_typeHandle);

            // cast to uint trick, which eliminates < 0 check
            if ((uint)index >= (uint)list->_count)
            {
                throw new IndexOutOfRangeException();
            }

            var items = list->_items;
            return items.Element<T>(index);
        }

        public static ref T GetRef<T>(UnsafeList* list, int index) where T : unmanaged
        {
            return ref *GetPtr<T>(list, index);
        }

        public static void RemoveAt(UnsafeList* list, int index)
        {
            UDebug.Assert(list != null);
            UDebug.Assert(list->_items.Ptr != null);

            var count = list->_count;

            // cast to uint trick, which eliminates < 0 check
            if ((uint)index >= (uint)count)
            {
                throw new ArgumentOutOfRangeException();
            }

            // reduce count
            list->_count = --count;

            // if index is still less than count, it means we removed an item 
            // not at the end of the list, and that we have to shift the items
            // down from (index+1, count-index) to (index, count-index)
            if (index < count)
            {
                UnsafeBuffer.Move(list->_items, index + 1, index, count - index);
            }
        }

        public static void RemoveAtUnordered(UnsafeList* list, int index)
        {
            UDebug.Assert(list != null);
            UDebug.Assert(list->_items.Ptr != null);

            var count = list->_count;

            // cast to uint trick, which eliminates < 0 check
            if ((uint)index >= (uint)count)
            {
                throw new ArgumentOutOfRangeException();
            }

            // reduce count
            list->_count = --count;

            if (index < count)
            {
                UnsafeBuffer.Move(list->_items, count, index, 1);
            }
        }

        public static bool Contains<T>(UnsafeList* list, T item) where T : unmanaged, IEquatable<T>
        {
            return IndexOf(list, item) > -1;
        }

        public static int IndexOf<T>(UnsafeList* list, T item) where T : unmanaged, IEquatable<T>
        {
            UDebug.Assert(list != null);
            UDebug.Assert(list->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == list->_typeHandle);

            return UnsafeBuffer.IndexOf(list->_items, item, 0, list->_count);
        }

        public static int LastIndexOf<T>(UnsafeList* list, T item) where T : unmanaged, IEquatable<T>
        {
            UDebug.Assert(list != null);
            UDebug.Assert(list->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == list->_typeHandle);

            return UnsafeBuffer.LastIndexOf(list->_items, item, 0, list->_count);
        }

        public static bool Remove<T>(UnsafeList* list, T item) where T : unmanaged, IEquatable<T>
        {
            UDebug.Assert(list != null);
            UDebug.Assert(list->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == list->_typeHandle);

            int index = IndexOf<T>(list, item);
            if (index < 0)
            {
                return false;
            }

            RemoveAt(list, index);
            return true;
        }

        public static bool RemoveUnordered<T>(UnsafeList* list, T item) where T : unmanaged, IEquatable<T>
        {
            int index = IndexOf(list, item);
            if (index < 0)
            {
                return false;
            }

            RemoveAtUnordered(list, index);
            return true;
        }

        public static Enumerator<T> GetEnumerator<T>(UnsafeList* list) where T : unmanaged
        {
            UDebug.Assert(list != null);
            UDebug.Assert(list->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == list->_typeHandle);

            return new Enumerator<T>(list->_items, 0, list->_count);
        }

        public unsafe struct Enumerator<T> : IUnsafeEnumerator<T> where T : unmanaged
        {
            T* _current;
            int _index;
            readonly int _count;
            readonly int _offset;
            UnsafeBuffer _buffer;

            internal Enumerator(UnsafeBuffer buffer, int offset, int count)
            {
                _index = 0;
                _count = count;
                _offset = offset;
                _buffer = buffer;
                _current = default;
            }

            public void Dispose()
            { }

            public bool MoveNext()
            {
                if ((uint)_index < (uint)_count)
                {
                    _current = _buffer.Element<T>((_offset + _index) % _buffer.Length);
                    _index++;
                    return true;
                }

                _current = default;
                return false;
            }

            public void Reset()
            {
                _index = 0;
                _current = default;
            }

            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    UDebug.Assert(_current != null);
                    return *_current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || _index == _count + 1)
                        throw new InvalidOperationException();

                    return Current;
                }
            }

            public Enumerator<T> GetEnumerator()
            {
                return this;
            }
            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                return this;
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return this;
            }
        }
    }
}