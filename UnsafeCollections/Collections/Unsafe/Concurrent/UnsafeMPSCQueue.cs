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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;


namespace UnsafeCollections.Collections.Unsafe.Concurrent
{
    public unsafe struct UnsafeMPSCQueue
    {
        // Implementation based on .Net Core3.1 ConcurrentQueue

        const string DESTINATION_TOO_SMALL = "Destination too small.";

        UnsafeBuffer _items;
        HeadAndTail _headAndTail;

        IntPtr _typeHandle; // Readonly
        int _mask;          // Readonly
        int _slotOffset;    // Readonly

        /// <summary>
        /// Allocates a new SPSCRingbuffer. Capacity will be set to a power of 2.
        /// </summary>
        public static UnsafeMPSCQueue* Allocate<T>(int capacity) where T : unmanaged
        {
            UDebug.Assert(capacity > 0);

            capacity = Memory.RoundUpToPowerOf2(capacity);

            // Required to get the memory size of the Slot + Value
            int slotStride = Marshal.SizeOf(new Slot<T>());
            int slotAlign = Memory.GetMaxAlignment(sizeof(T), sizeof(int));
            int slotOffset = Memory.RoundToAlignment(sizeof(T), slotAlign);

            int alignment = Memory.GetAlignment(slotStride);

            UnsafeMPSCQueue* queue;

            var sizeOfQueue = Memory.RoundToAlignment(sizeof(UnsafeMPSCQueue), alignment);
            var sizeOfArray = slotStride * capacity;

            var ptr = Memory.MallocAndZero(sizeOfQueue + sizeOfArray, alignment);

            // cast ptr to queue
            queue = (UnsafeMPSCQueue*)ptr;

            // initialize fixed buffer from same block of memory as the stack
            UnsafeBuffer.InitFixed(&queue->_items, (byte*)ptr + sizeOfQueue, capacity, slotStride);

            // Read-only values
            queue->_mask = capacity - 1;
            queue->_slotOffset = slotOffset;
            queue->_typeHandle = typeof(T).TypeHandle.Value;

            // Reset the queue for use.
            Clear(queue);

            return queue;
        }

        public static void Free(UnsafeMPSCQueue* queue)
        {
            if (queue == null)
                return;

            // clear queue memory (just in case)
            *queue = default;

            // free queue memory, if this is a fixed queue it frees the items memory at the same time
            Memory.Free(queue);
        }

        public static bool IsEmpty<T>(UnsafeMPSCQueue* queue) where T : unmanaged
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == queue->_typeHandle);

            var nextHead = Volatile.Read(ref queue->_headAndTail.Head) + 1;

            return (Volatile.Read(ref queue->_headAndTail.Tail) < nextHead);
        }

        public static int GetCapacity(UnsafeMPSCQueue* queue)
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);

            return queue->_items.Length;
        }

        /// <summary>
        /// Gets the current count of the queue.
        /// Value becomes stale if enqueue/dequeue operations happen.
        /// </summary>
        public static int GetCount(UnsafeMPSCQueue* queue)
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

                return head < tail ? tail - head : queue->_items.Length - head + tail;
            }
            return 0;
        }

        public static void Clear(UnsafeMPSCQueue* queue)
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);

            queue->_headAndTail = new HeadAndTail();

            // Initialize the sequence number for each slot.
            // This is used to synchronize between consumer and producer threads.
            var offset = queue->_slotOffset;
            var items = queue->_items;

            for (int i = 0; i < queue->_items.Length; i++)
                *(int*)items.Element(i, offset) = i;
        }

        /// <summary>
        /// Tries to enqueue an item in the queue. Returns false if there's no space in the queue.
        /// </summary>
        public static bool TryEnqueue<T>(UnsafeMPSCQueue* queue, T item) where T : unmanaged
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == queue->_typeHandle);

            SpinWait spinner = default;

            // Temp copy of inner buffer
            var items = queue->_items;
            var offset = queue->_slotOffset;

            while (true)
            {
                int tail = Volatile.Read(ref queue->_headAndTail.Tail);
                int index = tail & queue->_mask;

                int seq = Volatile.Read(ref *(int*)items.Element(index, offset));
                int dif = seq - tail;

                if (dif == 0)
                {
                    // Reserve the slot
                    if (Interlocked.CompareExchange(ref queue->_headAndTail.Tail, tail + 1, tail) == tail)
                    {
                        // Write the value and update the seq
                        *items.Element<T>(index) = item;
                        Volatile.Write(ref *(int*)items.Element(index, offset), tail + 1);
                        return true;
                    }
                }
                else if (dif < 0)
                {
                    // Slot was full
                    return false;
                }
                // Lost the race, try again
                spinner.SpinOnce();
            }
        }

        /// <summary>
        /// Dequeues an item from the queue. Blocks the thread until there is space in the queue.
        /// </summary>
        public static T Dequeue<T>(UnsafeMPSCQueue* queue) where T : unmanaged
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == queue->_typeHandle);

            SpinWait spinner = new SpinWait();

            // Temp copy of inner buffer
            var items = queue->_items;

            int head = Volatile.Read(ref queue->_headAndTail.Head);
            int index = head & queue->_mask;
            int offset = queue->_slotOffset;

            while (true)
            {
                int seq = Volatile.Read(ref *(int*)items.Element(index, offset));
                int dif = seq - (head + 1);

                if (dif == 0)
                {
                    // Update head
                    Volatile.Write(ref queue->_headAndTail.Head, head + 1);
                    var item = *items.Element<T>(index);

                    // Update slot after reading
                    Volatile.Write(ref *(int*)items.Element(index, offset), head + items.Length);

                    return item;
                }

                spinner.SpinOnce();
            }
        }

        /// <summary>
        /// Tries to dequeue an item from the queue. Returns false if there's no items in the queue.
        /// </summary>
        public static bool TryDequeue<T>(UnsafeMPSCQueue* queue, out T result) where T : unmanaged
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == queue->_typeHandle);

            // Temp copy of inner buffer
            var items = queue->_items;

            int head = Volatile.Read(ref queue->_headAndTail.Head);
            int index = head & queue->_mask;
            int offset = queue->_slotOffset;

            int seq = Volatile.Read(ref *(int*)items.Element(index, offset));
            int dif = seq - (head + 1);

            if (dif == 0)
            {
                // Update head
                Volatile.Write(ref queue->_headAndTail.Head, head + 1);
                result = *items.Element<T>(index);

                // Update slot after reading
                Volatile.Write(ref *(int*)items.Element(index, offset), head + items.Length);

                return true;
            }

            result = default;
            return false;
        }

        public static bool TryPeek<T>(UnsafeMPSCQueue* queue, out T result) where T : unmanaged
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == queue->_typeHandle);

            // Temp copy of inner buffer
            var items = queue->_items;

            int head = Volatile.Read(ref queue->_headAndTail.Head);
            int index = head & queue->_mask;
            int offset = queue->_slotOffset;

            int seq = Volatile.Read(ref *(int*)items.Element(index, offset));
            int dif = seq - (head + 1);

            if (dif == 0)
            {
                result = *items.Element<T>(index);
                return true;
            }

            result = default;
            return false;
        }

        /// <summary>
        /// Returns a snapshot of the elements.
        /// </summary>
        /// <returns></returns>
        internal static T[] ToArray<T>(UnsafeMPSCQueue* queue) where T : unmanaged
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == queue->_typeHandle);

            int count = GetCount(queue);
            var arr = new T[count];

            var enumerator = GetEnumerator<T>(queue);

            int i = 0;
            while (enumerator.MoveNext() && i < count)
                arr[i++] = enumerator.Current;


            return arr;
        }

        /// <summary>
        /// Creates an enumerator for the current snapshot of the queue.
        /// </summary>
        public static Enumerator<T> GetEnumerator<T>(UnsafeMPSCQueue* queue) where T : unmanaged
        {
            UDebug.Assert(queue != null);
            UDebug.Assert(queue->_items.Ptr != null);
            UDebug.Assert(typeof(T).TypeHandle.Value == queue->_typeHandle);

            return new Enumerator<T>(queue);
        }

        //https://source.dot.net/#System.Private.CoreLib/ConcurrentQueueSegment.cs,ec7a63152c0fbc9e

        [StructLayout(LayoutKind.Explicit, Size = 3 * CACHE_LINE_SIZE)]
        [DebuggerDisplay("Head = {Head}, Tail = {Tail}")]
        private struct HeadAndTail
        {
            private const int CACHE_LINE_SIZE = 64;

            [FieldOffset(1 * CACHE_LINE_SIZE)]
            public int Head;

            [FieldOffset(2 * CACHE_LINE_SIZE)]
            public int Tail;
        }

        // This struct is only used to get the size in memory
        [StructLayout(LayoutKind.Sequential)]
        private struct Slot<T>
        {
            T Item;
            int SequenceNumber;
        }


        public unsafe struct Enumerator<T> : IUnsafeEnumerator<T> where T : unmanaged
        {
            // Enumerates over the provided MPSCRingBuffer. Enumeration counts as a READ/Consume operation.
            // The amount of items enumerated can vary depending on if the TAIL moves during enumeration.
            // The HEAD is frozen in place when the enumerator is created. This means that the maximum 
            // amount of items read is always the capacity of the queue and no more.
            const string HEAD_MOVED_FAULT = "Enumerator was invalidated by dequeue operation!";

            readonly UnsafeMPSCQueue* _queue;
            readonly int _headStart;
            readonly int _mask;
            readonly int _seqOffset;
            int _index;
            T* _current;

            internal Enumerator(UnsafeMPSCQueue* queue)
            {
                _queue = queue;
                _index = -1;
                _current = default;
                _headStart = Volatile.Read(ref queue->_headAndTail.Head);
                _mask = queue->_mask;
                _seqOffset = queue->_slotOffset;
            }

            public void Dispose()
            {
                _index = -2;
                _current = default;
            }

            public bool MoveNext()
            {
                if (_index == -2)
                    return false;

                int head = Volatile.Read(ref _queue->_headAndTail.Head);
                if (_headStart != head)
                    throw new InvalidOperationException(HEAD_MOVED_FAULT);

                int headIndex = head + ++_index;
                int index = headIndex & _mask;

                int seq = Volatile.Read(ref *(int*)_queue->_items.Element(index, _seqOffset));
                int dif = seq - (headIndex + 1);

                if (dif == 0)
                {
                    _current = _queue->_items.Element<T>(index);
                    return true;
                }

                _current = default;
                return false;
            }

            public void Reset()
            {
                _index = -1;
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
                    if (_index < 0)
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
