/*
The MIT License (MIT)

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
using System.Runtime.InteropServices;
using System.Threading;

namespace UnsafeCollections.Collections.Unsafe.Concurrent
{
    /// <summary>
    /// A ringbuffer that acts as a queue. Buffer has a fixed size.
    /// </summary>
    public unsafe struct UnsafeSPSCRingbuffer
    {
        UnsafeBuffer _items;
        IntPtr _typeHandle;
        HeadAndTail _headAndTail;
        int _mask; //readonly

        /// <summary>
        /// Allocates a new SPSCRingbuffer. Capacity will be set to a power of 2.
        /// </summary>
        public static UnsafeSPSCRingbuffer* Allocate<T>(int capacity) where T : unmanaged
        {
            UDebug.Assert(capacity > 0);

            capacity = Memory.RoundUpToPowerOf2(capacity);
            int stride = sizeof(T);

            UnsafeSPSCRingbuffer* queue;

            var alignment = Memory.GetAlignment(stride);
            var sizeOfQueue = Memory.RoundToAlignment(sizeof(UnsafeSPSCRingbuffer), alignment);
            var sizeOfArray = stride * capacity;

            var ptr = Memory.MallocAndZero(sizeOfQueue + sizeOfArray, alignment);

            // cast ptr to queue
            queue = (UnsafeSPSCRingbuffer*)ptr;

            // initialize fixed buffer from same block of memory as the stack
            UnsafeBuffer.InitFixed(&queue->_items, (byte*)ptr + sizeOfQueue, capacity, stride);

            queue->_headAndTail = new HeadAndTail();
            queue->_mask = capacity - 1;
            queue->_typeHandle = typeof(T).TypeHandle.Value;

            return queue;
        }

        public static void Free(UnsafeSPSCRingbuffer* queue)
        {
            if (queue == null)
                return;

            // clear queue memory (just in case)
            *queue = default;

            // free queue memory, if this is a fixed queue it frees the items memory at the same time
            Memory.Free(queue);
        }

        public static bool IsEmpty<T>(UnsafeSPSCRingbuffer* queue) where T : unmanaged
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == queue->_typeHandle);

            var nextHead = Volatile.Read(ref queue->_headAndTail.Head) + 1;

            return (Volatile.Read(ref queue->_headAndTail.Tail) < nextHead);
        }

        public static int GetCapacity(UnsafeSPSCRingbuffer* queue)
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);

            return queue->_items.Length;
        }

        public static int GetCount(UnsafeSPSCRingbuffer* queue)
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);

            var head = Volatile.Read(ref queue->_headAndTail.Head);
            var tail = Volatile.Read(ref queue->_headAndTail.Tail);
            int mask = queue->_mask;

            if (head != tail)
            {
                head &= mask;
                tail &= mask;

                return (int)(head < tail ? tail - head : queue->_items.Length - head + tail);
            }
            return 0;
        }

        public static void Clear(UnsafeSPSCRingbuffer* queue)
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);

            queue->_headAndTail = new HeadAndTail();
        }

        /// <summary>
        /// Enqueues an item in the queue. Blocks the thread until there is space in the queue.
        /// </summary>
        public static void Enqueue<T>(UnsafeSPSCRingbuffer* queue, T item) where T : unmanaged
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == queue->_typeHandle);

            SpinWait spinner = default;
            var nextTail = Volatile.Read(ref queue->_headAndTail.Tail) + 1;
            var currentHead = Volatile.Read(ref queue->_headAndTail.Head);

            var wrap = nextTail - queue->_items.Length;

            while (wrap > currentHead)
            {
                //Full queue, wait for space
                currentHead = Volatile.Read(ref queue->_headAndTail.Head);
                spinner.SpinOnce();
            }

            int nextIndex = (int)(nextTail & queue->_mask);
            *queue->_items.Element<T>(nextIndex) = item;

            Volatile.Write(ref queue->_headAndTail.Tail, nextTail);
        }

        /// <summary>
        /// Tries to enqueue an item in the queue. Returns false if there's no space in the queue.
        /// </summary>
        public static bool TryEnqueue<T>(UnsafeSPSCRingbuffer* queue, T item) where T : unmanaged
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == queue->_typeHandle);

            var nextTail = Volatile.Read(ref queue->_headAndTail.Tail) + 1;
            var currentHead = Volatile.Read(ref queue->_headAndTail.Head);

            var wrap = nextTail - queue->_items.Length;

            if (wrap > currentHead)
            {
                return false;
            }

            int nextIndex = (int)(nextTail & queue->_mask);
            *queue->_items.Element<T>(nextIndex) = item;

            Volatile.Write(ref queue->_headAndTail.Tail, nextTail);
            return true;
        }

        /// <summary>
        /// Dequeues an item from the queue. Blocks the thread until there is space in the queue.
        /// </summary>
        public static T Dequeue<T>(UnsafeSPSCRingbuffer* queue) where T : unmanaged
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == queue->_typeHandle);

            SpinWait spinner = default;
            var nextHead = Volatile.Read(ref queue->_headAndTail.Head) + 1;

            while (Volatile.Read(ref queue->_headAndTail.Tail) < nextHead)
            {
                spinner.SpinOnce();
            }

            int nextIndex = (int)(nextHead & queue->_mask);
            var result = *queue->_items.Element<T>(nextIndex);
            Volatile.Write(ref queue->_headAndTail.Head, nextHead);

            return result;
        }

        /// <summary>
        /// Tries to dequeue an item from the queue. Returns false if there's no items in the queue.
        /// </summary>
        public static bool TryDequeue<T>(UnsafeSPSCRingbuffer* queue, out T result) where T : unmanaged
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == queue->_typeHandle);

            var nextHead = Volatile.Read(ref queue->_headAndTail.Head) + 1;

            if (Volatile.Read(ref queue->_headAndTail.Tail) < nextHead)
            {
                result = default;
                return false;
            }

            int nextIndex = (int)(nextHead & queue->_mask);
            result = *queue->_items.Element<T>(nextIndex);
            Volatile.Write(ref queue->_headAndTail.Head, nextHead);

            return true;
        }

        public static bool TryPeek<T>(UnsafeSPSCRingbuffer* queue, out T result) where T : unmanaged
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == queue->_typeHandle);

            var nextHead = Volatile.Read(ref queue->_headAndTail.Head) + 1;

            if (Volatile.Read(ref queue->_headAndTail.Tail) < nextHead)
            {
                result = default;
                return false;
            }

            int nextIndex = (int)(nextHead & queue->_mask);
            result = *queue->_items.Element<T>(nextIndex);

            return true;
        }

        /// <summary>
        /// Copies the current snapshot of the queue.
        /// </summary>
        internal static void CopyTo<T>(UnsafeSPSCRingbuffer* queue, void* destination, int destinationIndex) where T : unmanaged
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == queue->_typeHandle);
            UDebug.Assert(destination != null);
            UDebug.Always(destinationIndex > -1);


            var head = Volatile.Read(ref queue->_headAndTail.Head);
            var tail = Volatile.Read(ref queue->_headAndTail.Tail);
            var mask = queue->_mask;

            head &= mask;
            tail &= mask;

            var count = head < tail ?
                tail - head :
                queue->_items.Length - head + tail;


        }

        /// <summary>
        /// Creates an enumerator for the current snapshot of the queue.
        /// </summary>
        public static Enumerator<T> GetEnumerator<T>(UnsafeSPSCRingbuffer* queue) where T : unmanaged
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == queue->_typeHandle);
            //TODO
            throw new NotImplementedException();
        }


        //https://source.dot.net/#System.Private.CoreLib/ConcurrentQueueSegment.cs,ec7a63152c0fbc9e
        //https://software.intel.com/content/www/us/en/develop/articles/single-producer-single-consumer-queue.html

        [StructLayout(LayoutKind.Explicit, Size = 3 * CACHE_LINE_SIZE)]
        private struct HeadAndTail
        {
            private const int CACHE_LINE_SIZE = 64;

            [FieldOffset(1 * CACHE_LINE_SIZE)]
            public long Head;

            [FieldOffset(2 * CACHE_LINE_SIZE)]
            public long Tail;
        }

        public unsafe struct Enumerator<T> : IUnsafeEnumerator<T> where T : unmanaged
        {
            public T Current => throw new NotImplementedException();

            object IEnumerator.Current => throw new NotImplementedException();

            public void Dispose()
            {
                throw new NotImplementedException();
            }

            public IEnumerator<T> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            public bool MoveNext()
            {
                throw new NotImplementedException();
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }
    }
}
