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
using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;

namespace Collections.Unsafe {
  unsafe struct UnsafeBuffer {
    [NativeDisableUnsafePtrRestriction]
    public void* Ptr;

    public int  Length;
    public int  Stride;
    public bool Dynamic;

    public static void Free(UnsafeBuffer* buffer) {
      Assert.Check(buffer != null);

      if (buffer->Dynamic == false) {
        throw new InvalidOperationException("Can't free a fixed buffer");
      }

      // buffer ptr can't be null
      Assert.Check(buffer->Ptr != null);

      // free memory of ptr
      Native.Free(buffer->Ptr);

      // clear buffer itself
      *buffer = default;
    }

    public static void Clear(UnsafeBuffer* buffer)
    {
      Native.MemClear(buffer->Ptr, buffer->Length * buffer->Stride);
    }

    public static void InitFixed(UnsafeBuffer* buffer, void* ptr, int length, int stride) {
      Assert.Check(buffer != null);
      Assert.Check(ptr != null);

      Assert.Check(length > 0);
      Assert.Check(stride > 0);

      // ensure alignment of fixed buffer
      Assert.Check((((IntPtr)ptr).ToInt64() % Native.GetAlignment(stride)) == 0);

      buffer->Ptr     = ptr;
      buffer->Length  = length;
      buffer->Stride  = stride;
      buffer->Dynamic = false;
    }

    public static void InitDynamic<T>(UnsafeBuffer* buffer, int length) where T : unmanaged {
      InitDynamic(buffer, length, sizeof(T));
    }

    public static void InitDynamic(UnsafeBuffer* buffer, int length, int stride) {
      Assert.Check(buffer != null);
      Assert.Check(length > 0);
      Assert.Check(stride > 0);

      buffer->Ptr     = Native.MallocAndClear(length * stride, Native.GetAlignment(stride));
      buffer->Length  = length;
      buffer->Stride  = stride;
      buffer->Dynamic = true;
    }

    public static void Copy(UnsafeBuffer source, int sourceIndex, UnsafeBuffer destination, int destinationIndex, int count) {
      Assert.Check(source.Ptr != null);
      Assert.Check(source.Ptr != destination.Ptr);
      Assert.Check(source.Stride == destination.Stride);
      Assert.Check(source.Stride > 0);
      Assert.Check(destination.Ptr != null);
      Native.MemCpy((byte*)destination.Ptr + (destinationIndex * source.Stride), (byte*)source.Ptr + (sourceIndex * source.Stride), count * source.Stride);
    }

    public static void Move(UnsafeBuffer source, int fromIndex, int toIndex, int count) {
      Assert.Check(source.Ptr != null);
      Native.MemMove((byte*)source.Ptr + (toIndex * source.Stride), (byte*)source.Ptr + (fromIndex * source.Stride), count * source.Stride);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void* Element(void* bufferPtr, int index, int stride) {
      return (byte*)bufferPtr + (index * stride);
    }
  }
}