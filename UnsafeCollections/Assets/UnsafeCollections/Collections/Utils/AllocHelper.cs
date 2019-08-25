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
using Unity.Collections.LowLevel.Unsafe;

namespace Collections.Unsafe {
  static unsafe class AllocHelper {
    public const int CACHE_LINE_SIZE = 64;
    
    public static void MemMove(void* destination, void* source, long size) {
      UnsafeUtility.MemMove(destination, source, size);
    }

    public static void MemCpy(void* destination, void* source, long size) {
      UnsafeUtility.MemCpy(destination, source, size);
    }

    public static void Copy(void* source, int sourceIndex, void* destination, int destinationIndex, int count, int elementSize) {
      UnsafeUtility.MemCpy(((byte*)destination) + (destinationIndex * elementSize), ((byte*)source) + (sourceIndex * elementSize), count * elementSize);
    }

    public static void* MallocAndClear(long size) {
      var memory = UnsafeUtility.Malloc(size, 4, Unity.Collections.Allocator.Persistent);
      UnsafeUtility.MemClear(memory, size);
      return memory;
    }

    public static void* MallocAndClear(long size, int alignment) {
      var memory = UnsafeUtility.Malloc(size, alignment, Unity.Collections.Allocator.Persistent);
      UnsafeUtility.MemClear(memory, size);
      return memory;
    }

    public static void* Malloc(long size) {
      return UnsafeUtility.Malloc(size, 4, Unity.Collections.Allocator.Persistent);
    }

    public static void MemClear(void* ptr, long size) {
      UnsafeUtility.MemClear(ptr, size);
    }

    public static T* MallocAndClear<T>() where T : unmanaged {
      var memory = UnsafeUtility.Malloc(sizeof(T), 4, Unity.Collections.Allocator.Persistent);
      UnsafeUtility.MemClear(memory, sizeof(T));
      return (T*)memory;
    }

    public static T* Malloc<T>() where T : unmanaged {
      return (T*)UnsafeUtility.Malloc(sizeof(T), 4, Unity.Collections.Allocator.Persistent);
    }

    public static void Free(void* memory) {
      UnsafeUtility.Free(memory, Unity.Collections.Allocator.Persistent);
    }

    public static int RoundUpToAlignment(int value) {
      return RoundUpToAlignment(value, 4);
    }

    public static int RoundUpToAlignment(int size, int alignment) {
      switch (alignment) {
        case 1:  return size;
        case 2:  return ((size + 1) >> 1) * 4;
        case 4:  return ((size + 3) >> 2) * 4;
        case 8:  return ((size + 7) >> 3) * 8;
        case 16: return ((size + 15) >> 4) * 16;
        case 32: return ((size + 31) >> 5) * 32;
        case 64: return ((size + 63) >> 6) * 64;
        default:
          throw new InvalidOperationException($"Invalid Alignment: {alignment}");
      }
    }

    public static int GetAlignmentForArrayElement(int elementSize) {
      switch (elementSize) {
        case 8:  return 8;
        case 16: return 16;
        case 32: return 32;
        case 64: return 64;
        default: return 4;
      }
    }
  }
}