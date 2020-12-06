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
using UnsafeCollections;
using UnsafeCollections.Unsafe;

namespace UnsafeCollections.Collections.Unsafe
{
    public unsafe struct UnsafeRingBuffer
    {
        UnsafeBuffer _items;

        int _head;
        int _tail;
        int _count;
        int _overwrite;

        public static UnsafeRingBuffer* Allocate<T>(int capacity, bool overwrite) where T : unmanaged
        {
            return Allocate(capacity, sizeof(T), overwrite);
        }

        public static UnsafeRingBuffer* Allocate(int capacity, int stride, bool overwrite)
        {
            UDebug.Assert(capacity > 0);
            UDebug.Assert(stride > 0);

            // fixedSize means we are allocating the memory for the collection header and the items in it as one block
            var alignment = Memory.GetAlignment(stride);

            // align header size to the elements alignment
            var sizeOfHeader = Memory.RoundToAlignment(sizeof(UnsafeRingBuffer), alignment);
            var sizeOfBuffer = stride * capacity;

            // allocate memory for list and array with the correct alignment
            var ptr = Memory.MallocAndZero(sizeOfHeader + sizeOfBuffer, alignment);

            // grab header ptr
            var ring = (UnsafeRingBuffer*)ptr;

            // initialize fixed buffer from same block of memory as the collection, offset by sizeOfHeader
            UnsafeBuffer.InitFixed(&ring->_items, (byte*)ptr + sizeOfHeader, capacity, stride);

            // initialize count to 0
            ring->_count = 0;
            ring->_overwrite = overwrite ? 1 : 0;
            return ring;
        }

        public static void Free(UnsafeRingBuffer* ring)
        {
            if (ring == null)
                return;

            // clear memory just in case
            *ring = default;

            // release ring memory
            Memory.Free(ring);
        }

        public static int GetCapacity(UnsafeRingBuffer* ring)
        {
            UDebug.Assert(ring != null);
            UDebug.Assert(ring->_items.Ptr != null);
            return ring->_items.Length;
        }

        public static int GetCount(UnsafeRingBuffer* ring)
        {
            UDebug.Assert(ring != null);
            UDebug.Assert(ring->_items.Ptr != null);
            return ring->_count;
        }

        public static void Clear(UnsafeRingBuffer* ring)
        {
            UDebug.Assert(ring != null);
            UDebug.Assert(ring->_items.Ptr != null);

            ring->_tail = 0;
            ring->_head = 0;
            ring->_count = 0;
        }

        public static bool IsFull(UnsafeRingBuffer* ring)
        {
            UDebug.Assert(ring != null);
            UDebug.Assert(ring->_items.Ptr != null);
            return ring->_count == ring->_items.Length;
        }

        public static void Set<T>(UnsafeRingBuffer* ring, int index, T value) where T : unmanaged
        {
            // cast to uint trick, which eliminates < 0 check
            if ((uint)index >= (uint)ring->_count)
            {
                throw new IndexOutOfRangeException();
            }

            // assign element
            *(T*)UnsafeBuffer.Element(ring->_items.Ptr, (ring->_tail + index) % ring->_items.Length, ring->_items.Stride) = value;
        }

        public static T Get<T>(UnsafeRingBuffer* ring, int index) where T : unmanaged
        {
            return *GetPtr<T>(ring, index);
        }

        public static T* GetPtr<T>(UnsafeRingBuffer* ring, int index) where T : unmanaged
        {
            // cast to uint trick, which eliminates < 0 check
            if ((uint)index >= (uint)ring->_count)
            {
                throw new IndexOutOfRangeException();
            }

            return (T*)UnsafeBuffer.Element(ring->_items.Ptr, (ring->_tail + index) % ring->_items.Length, ring->_items.Stride);
        }

        public static ref T GetRef<T>(UnsafeRingBuffer* ring, int index) where T : unmanaged
        {
            return ref *GetPtr<T>(ring, index);
        }


        public static bool Push<T>(UnsafeRingBuffer* ring, T item) where T : unmanaged
        {
            if (ring->_count == ring->_items.Length)
            {
                if (ring->_overwrite == 1)
                {
                    ring->_tail = (ring->_tail + 1) % ring->_items.Length;
                    ring->_count = (ring->_count - 1);
                }
                else
                {
                    return false;
                }
            }

            // store value at head
            *(T*)UnsafeBuffer.Element(ring->_items.Ptr, ring->_head, ring->_items.Stride) = item;

            // move head pointer forward
            ring->_head = (ring->_head + 1) % ring->_items.Length;

            // add count
            ring->_count += 1;

            // success!
            return true;
        }

        public static bool Pop<T>(UnsafeRingBuffer* ring, out T value) where T : unmanaged
        {
            UDebug.Assert(ring != null);
            UDebug.Assert(ring->_items.Ptr != null);

            if (ring->_count == 0)
            {
                value = default;
                return false;
            }

            // copy item from tail
            value = *(T*)UnsafeBuffer.Element(ring->_items.Ptr, ring->_tail, ring->_items.Stride);

            // move tail forward and decrement count
            ring->_tail = (ring->_tail + 1) % ring->_items.Length;
            ring->_count = (ring->_count - 1);
            return true;
        }

        public static ListIterator<T> GetIterator<T>(UnsafeRingBuffer* buffer) where T : unmanaged
        {
            return new ListIterator<T>(buffer->_items, buffer->_tail, buffer->_count);
        }
    }
}