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
using UnsafeCollections.Unsafe;

namespace UnsafeCollections.Collections
{
    public unsafe partial struct UnsafeQueue
    {
        const string QUEUE_EMPTY = "Queue is empty";
        const string QUEUE_FIXED_SIZE_FULL = "Fixed size queue full";

        UnsafeBuffer _items;
        int _count;

        int _head;
        int _tail;

        public static UnsafeQueue* Allocate<T>(int capacity, bool fixedSize = false) where T : unmanaged
        {
            return Allocate(capacity, sizeof(T), fixedSize);
        }

        public static UnsafeQueue* Allocate(int capacity, int stride, bool fixedSize = false)
        {
            UDebug.Assert(capacity > 0);
            UDebug.Assert(stride > 0);

            UnsafeQueue* queue;

            // fixedSize queue means we are allocating the memory for the header and the items in it as one block
            if (fixedSize)
            {
                var alignment = Memory.GetAlignment(stride);
                var sizeOfQueue = Memory.RoundToAlignment(sizeof(UnsafeQueue), alignment);
                var sizeOfArray = stride * capacity;

                var ptr = Memory.MallocAndZero(sizeOfQueue + sizeOfArray, alignment);

                // cast ptr to queue
                queue = (UnsafeQueue*)ptr;

                // initialize fixed buffer from same block of memory as the stack
                UnsafeBuffer.InitFixed(&queue->_items, (byte*)ptr + sizeOfQueue, capacity, stride);
            }

            // dynamic sized queue means we're allocating the stack header and its memory separately
            else
            {
                // allocate memory for queue
                queue = Memory.MallocAndZero<UnsafeQueue>();

                // initialize dynamic buffer with separate memory
                UnsafeBuffer.InitDynamic(&queue->_items, capacity, stride);
            }

            queue->_head = 0;
            queue->_tail = 0;
            queue->_count = 0;

            return queue;
        }

        public static void Free(UnsafeQueue* queue)
        {
            if (queue == null)
            {
                return;
            }

            // not fixed, we need to free items separtely 
            if (queue->_items.Dynamic == 1)
            {
                UnsafeBuffer.Free(&queue->_items);
            }

            // clear queue memory (just in case)
            *queue = default;

            // free queue memory, if this is a fixed queue it frees the items memory at the same time
            Memory.Free(queue);
        }

        public static int Capacity(UnsafeQueue* queue)
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);
            return queue->_items.Length;
        }

        public static int Count(UnsafeQueue* queue)
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);
            return queue->_count;
        }

        public static void Clear(UnsafeQueue* queue)
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);

            queue->_head = 0;
            queue->_tail = 0;
        }

        public static bool IsFixedSize(UnsafeQueue* queue)
        {
            UDebug.Assert(queue != null);
            return queue->_items.Dynamic == 0;
        }

        public static void Enqueue<T>(UnsafeQueue* queue, T item) where T : unmanaged
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);

            var count = queue->_count;
            var items = queue->_items;

            if (count == items.Length)
            {
                if (items.Dynamic == 1)
                {
                    Expand(queue, items.Length * 2);

                    // re-assign items after capacity expanded
                    items = queue->_items;
                }
                else
                {
                    throw new InvalidOperationException(QUEUE_FIXED_SIZE_FULL);
                }
            }

            var tail = queue->_tail;

            *(T*)UnsafeBuffer.Element(items.Ptr, tail, items.Stride) = item;

            // increment count and head index
            queue->_count = (count + 1);
            queue->_tail = (tail + 1) % items.Length;
        }

        public static bool TryEnqueue<T>(UnsafeQueue* queue, T item) where T : unmanaged
        {
            if (queue->_count == queue->_items.Length && queue->_items.Dynamic == 0)
            {
                return false;
            }

            Enqueue(queue, item);
            return true;
        }

        public static T Dequeue<T>(UnsafeQueue* queue) where T : unmanaged
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);

            var count = queue->_count;
            if (count == 0)
            {
                throw new InvalidOperationException(QUEUE_EMPTY);
            }

            var head = queue->_head;
            var items = queue->_items;

            // grab result
            T result = *(T*)UnsafeBuffer.Element(items.Ptr, head, items.Stride);

            // decrement count and tail index
            queue->_count = (count - 1);
            queue->_head = (head + 1) % items.Length;
            return result;
        }

        public static bool TryDequeue<T>(UnsafeQueue* queue, out T result) where T : unmanaged
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);

            if (queue->_count == 0)
            {
                result = default;
                return false;
            }

            // grab result
            result = Dequeue<T>(queue);
            return true;
        }

        public static bool TryPeek<T>(UnsafeQueue* queue, out T result) where T : unmanaged
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);

            if (queue->_count == 0)
            {
                result = default;
                return false;
            }

            result = *PeekPtr<T>(queue);
            return true;
        }

        public static T Peek<T>(UnsafeQueue* queue) where T : unmanaged
        {
            return *PeekPtr<T>(queue);
        }

        public static T* PeekPtr<T>(UnsafeQueue* queue) where T : unmanaged
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);

            if (queue->_count == 0)
            {
                throw new InvalidOperationException(QUEUE_EMPTY);
            }

            var items = queue->_items;
            return (T*)UnsafeBuffer.Element(items.Ptr, queue->_head, items.Stride);
        }

        public static ref T PeekRef<T>(UnsafeQueue* queue) where T : unmanaged
        {
            return ref *PeekPtr<T>(queue);
        }

        // expand algorithm from first answer here: https://codereview.stackexchange.com/questions/129819/queue-resizing-array-implementation
        static void Expand(UnsafeQueue* queue, int capacity)
        {
            UDebug.Assert(capacity > 0);

            // queue has to be dynamic and capacity we're going to have to be larger
            UDebug.Assert(queue->_items.Dynamic == 1);
            UDebug.Assert(queue->_items.Length < capacity);

            // new buffer for elements
            UnsafeBuffer newItems = default;

            // initialize to double size of existing one
            UnsafeBuffer.InitDynamic(&newItems, capacity, queue->_items.Stride);

            if (queue->_count > 0)
            {
                // when head is 'ahead' or at tail it means that we're wrapping around 
                if (queue->_head >= queue->_tail)
                {
                    // so we need to copy head first, from (head, length-head) into (0, length-head) 
                    UnsafeBuffer.Copy(queue->_items, queue->_head, newItems, 0, queue->_items.Length - queue->_head);

                    // and then copy tail, from (0, tail) into (length-head, tail)  
                    UnsafeBuffer.Copy(queue->_items, 0, newItems, queue->_items.Length - queue->_head, queue->_tail);
                }
                else
                {
                    // if not, we can just copy from (tail, count) into (0, count) 
                    UnsafeBuffer.Copy(queue->_items, queue->_head, newItems, 0, queue->_count);
                }
            }

            // free existing buffer
            UnsafeBuffer.Free(&queue->_items);

            queue->_items = newItems;
            queue->_head = 0;
            queue->_tail = queue->_count % queue->_items.Length;
        }

        public static ListIterator<T> GetIterator<T>(UnsafeQueue* queue) where T : unmanaged
        {
            return new ListIterator<T>(queue->_items, queue->_head, queue->_count);
        }
    }
}