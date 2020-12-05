using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if !UNITY
using Unity.Collections.LowLevel.Unsafe;
#endif

namespace UnsafeCollections.Unsafe
{
    public static unsafe class Memory
    {
        public const int CACHE_LINE_SIZE = 64;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* Malloc(long size, int alignment = 8)
        {
#if UNITY
            return UnsafeUtility.Malloc(size, alignment, Unity.Collections.Allocator.Persistent);
#else
            //Marshal always allocates with an alignment of 8.
            return (void*)Marshal.AllocHGlobal((int)size);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* Malloc<T>() where T : unmanaged
        {
            return (T*)Malloc(sizeof(T), GetAlignment<T>());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Free(void* memory)
        {
#if UNITY
            UnsafeUtility.Free(memory, Unity.Collections.Allocator.Persistent);
#else
            Marshal.FreeHGlobal((IntPtr)memory);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ZeroMem(void* ptr, long size)
        {
#if UNITY
            UnsafeUtility.MemClear(ptr, size);
#else
            long c = size / 8; //longs
            long b = size % 8; //bytes

            for (int i = 0; i < c; i++)
                *((ulong*)ptr + i) = 0;

            for (int i = 0; i < b; i++)
                *((byte*)ptr + i) = 0;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* MallocAndZero(int size, int alignment = 8)
        {
            return MallocAndZero((long)size, alignment);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* MallocAndZero(long size, int alignment = 8)
        {
            var memory = Malloc(size, alignment);
            ZeroMem(memory, size);
            return memory;
        }


        public static T* MallocAndZero<T>() where T : unmanaged
        {
            var memory = Malloc(sizeof(T), GetAlignment<T>());
            ZeroMem(memory, sizeof(T));
            return (T*)memory;
        }

        public static T* MallocAndZeroArray<T>(int length) where T : unmanaged
        {
            var ptr = Malloc(sizeof(T) * length, GetAlignment<T>());
            ZeroMem(ptr, sizeof(T) * length);
            return (T*)ptr;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MemCpy(void* destination, void* source, int size)
        {
#if UNITY
            UnsafeUtility.MemCpy(destination, source, size);
#else
            Buffer.MemoryCopy(source, destination, size, size);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MemMove(void* destination, void* source, int size)
        {
#if !UNITY
            UnsafeUtility.MemMove(destination, source, size);
#else
            Buffer.MemoryCopy(source, destination, size, size);
#endif
        }

        public static void ArrayCopy<T>(void* source, int sourceIndex, void* destination, int destinationIndex, int count) where T : unmanaged
        {
            ArrayCopy(source, sourceIndex, destination, destinationIndex, count, sizeof(T));
        }

        public static void ArrayCopy(void* source, int sourceIndex, void* destination, int destinationIndex, int count, int elementStride)
        {
            MemCpy(((byte*)destination) + (destinationIndex * elementStride), ((byte*)source) + (sourceIndex * elementStride), count * elementStride);
        }

        public static void ZeroArray<T>(T* ptr, int size) where T : unmanaged
        {
            ZeroMem(ptr, sizeof(T) * size);
        }


        public static void* ExpandZeroed(void* buffer, int currentSize, int newSize)
        {
            UDebug.Assert(newSize > currentSize);

            var oldBuffer = buffer;
            var newBuffer = MallocAndZero(newSize);

            // copy old contents
            MemCpy(newBuffer, oldBuffer, currentSize);

            // free old buffer
            Free(oldBuffer);

            // return the new size
            return newBuffer;
        }

        public static void MemCpyFast(void* d, void* s, int size)
        {
            switch (size)
            {
                case 4:
                    *(uint*)d = *(uint*)s;
                    break;

                case 8:
                    *(ulong*)d = *(ulong*)s;
                    break;

                case 12:
                    *((ulong*)d) = *((ulong*)s);
                    *(((uint*)d) + 2) = *(((uint*)s) + 2);
                    break;

                case 16:
                    *((ulong*)d) = *((ulong*)s);
                    *((ulong*)d + 1) = *((ulong*)s + 1);
                    break;

                default:
                    MemCpy(d, s, size);
                    break;
            }
        }


        public static int RoundToAlignment(int stride, int alignment)
        {
            switch (alignment)
            {
                case 1: return stride;
                case 2: return ((stride + 1) >> 1) * 2;
                case 4: return ((stride + 3) >> 2) * 4;
                case 8: return ((stride + 7) >> 3) * 8;
                default:
                    throw new InvalidOperationException($"Invalid Alignment: {alignment}");
            }
        }

        public static int GetAlignment<T>() where T : unmanaged
        {
            return GetAlignment(sizeof(T));
        }

        public static int GetAlignment(int stride)
        {
            if ((stride % 8) == 0)
            {
                return 8;
            }

            if ((stride % 4) == 0)
            {
                return 4;
            }

            return (stride % 2) == 0 ? 2 : 1;
        }

        public static int GetMaxAlignment(int a, int b)
        {
            return Math.Max(GetAlignment(a), GetAlignment(b));
        }

        public static int GetMaxAlignment(int a, int b, int c)
        {
            return Math.Max(GetMaxAlignment(a, b), GetAlignment(c));
        }

        public static int GetMaxAlignment(int a, int b, int c, int d)
        {
            return Math.Max(GetMaxAlignment(a, b, c), GetAlignment(d));
        }

        public static int GetMaxAlignment(int a, int b, int c, int d, int e)
        {
            return Math.Max(GetMaxAlignment(a, b, c, e), GetAlignment(e));
        }
    }
}
