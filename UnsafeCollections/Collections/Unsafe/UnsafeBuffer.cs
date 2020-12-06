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
using System.Runtime.CompilerServices;
using UnsafeCollections.Unsafe;
#if UNITY
using Unity.Collections.LowLevel.Unsafe;
#endif

namespace UnsafeCollections.Collections.Unsafe
{
    internal unsafe struct UnsafeBuffer
    {
#if UNITY
        [NativeDisableUnsafePtrRestriction]
#endif
        internal void* Ptr;

        internal int Length;
        internal int Stride;
        internal int Dynamic;

        public static void Free(UnsafeBuffer* buffer)
        {
            if (buffer == null)
                return;

            if (buffer->Dynamic == 0)
                throw new InvalidOperationException("Can't free a fixed buffer");

            // buffer ptr can't be null
            UDebug.Assert(buffer->Ptr != null);

            // free memory of ptr
            Memory.Free(buffer->Ptr);

            // clear buffer itself
            *buffer = default;
        }

        public static void Clear(UnsafeBuffer* buffer)
        {
            Memory.ZeroMem(buffer->Ptr, buffer->Length * buffer->Stride);
        }

        public static void InitFixed<T>(UnsafeBuffer* buffer, void* ptr, int length) where T : unmanaged
        {
            InitFixed(buffer, ptr, length, sizeof(T));
        }

        public static void InitFixed(UnsafeBuffer* buffer, void* ptr, int length, int stride)
        {
            UDebug.Assert(buffer != null);
            UDebug.Assert(ptr != null);

            UDebug.Assert(length > 0);
            UDebug.Assert(stride > 0);

            // ensure alignment of fixed buffer
            UDebug.Assert((((IntPtr)ptr).ToInt64() % Memory.GetAlignment(stride)) == 0);

            buffer->Ptr = ptr;
            buffer->Length = length;
            buffer->Stride = stride;
            buffer->Dynamic = 0;
        }

        public static void InitDynamic<T>(UnsafeBuffer* buffer, int length) where T : unmanaged
        {
            InitDynamic(buffer, length, sizeof(T));
        }

        public static void InitDynamic(UnsafeBuffer* buffer, int length, int stride)
        {
            UDebug.Assert(buffer != null);
            UDebug.Assert(length > 0);
            UDebug.Assert(stride > 0);

            buffer->Ptr = Memory.MallocAndZero(length * stride, Memory.GetAlignment(stride));
            buffer->Length = length;
            buffer->Stride = stride;
            buffer->Dynamic = 1;
        }

        public static void Copy(UnsafeBuffer source, int sourceIndex, UnsafeBuffer destination, int destinationIndex, int count)
        {
            UDebug.Assert(source.Ptr != null);
            UDebug.Assert(source.Ptr != destination.Ptr);
            UDebug.Assert(source.Stride == destination.Stride);
            UDebug.Assert(source.Stride > 0);
            UDebug.Assert(destination.Ptr != null);
            Memory.MemCpy((byte*)destination.Ptr + (destinationIndex * source.Stride), (byte*)source.Ptr + (sourceIndex * source.Stride), count * source.Stride);
        }

        public static void Move(UnsafeBuffer source, int fromIndex, int toIndex, int count)
        {
            UDebug.Assert(source.Ptr != null);
            Memory.MemMove((byte*)source.Ptr + (toIndex * source.Stride), (byte*)source.Ptr + (fromIndex * source.Stride), count * source.Stride);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* Element(void* bufferPtr, int index, int stride)
        {
            return (byte*)bufferPtr + (index * stride);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void* Element(int index)
        {
            return (byte*)Ptr + (index * Stride);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T* Element<T>(int index) where T : unmanaged
        {
            return (T*)((byte*)Ptr + (index * Stride));
        }
    }
}