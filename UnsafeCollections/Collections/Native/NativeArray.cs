using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnsafeCollections.Collections.Unsafe;
#if UNITY
using Unity.Collections.LowLevel.Unsafe;
#endif

namespace UnsafeCollections.Collections.Native
{
    [DebuggerDisplay("Length = {Length}")]
    [DebuggerTypeProxy(typeof(Debug.TypeProxies.NativeArrayDebugView<>))]
    public unsafe struct NativeArray<T> : IDisposable, IEnumerable<T>, IEnumerable where T : unmanaged
    {
        private UnsafeArray* m_inner;

        public bool IsCreated
        {
            get
            {
                return m_inner != null;
            }
        }
        public int Length
        {
            get
            {
                if (m_inner == null)
                    throw new InvalidOperationException();
                return UnsafeArray.GetLength(m_inner);
            }
        }

        public NativeArray(int length)
        {
            m_inner = UnsafeArray.Allocate<T>(length);
        }

        public NativeArray(T[] array)
        {
            m_inner = UnsafeArray.Allocate<T>(array.Length);

            for (int i = 0; i < array.Length; i++)
                UnsafeArray.Set<T>(m_inner, i, array[i]);
        }

        public NativeArray(NativeArray<T> array)
        {
            m_inner = UnsafeArray.Allocate<T>(array.Length);

            Copy(array, this);
        }


        public T this[int index]
        {
            get
            {
                return UnsafeArray.Get<T>(m_inner, index);
            }
            set
            {
                UnsafeArray.Set<T>(m_inner, index, value);
            }
        }


        public static void Copy(NativeArray<T> src, NativeArray<T> dst)
        {
            Copy(src, 0, dst, 0, src.Length);
        }
        public static void Copy(NativeArray<T> src, NativeArray<T> dst, int length)
        {
            Copy(src, 0, dst, 0, length);
        }
        public static void Copy(NativeArray<T> src, int srcIndex, NativeArray<T> dst, int dstIndex, int length)
        {
            UnsafeArray.Copy<T>(src.m_inner, srcIndex, dst.m_inner, dstIndex, length);
        }

        public static void Copy(NativeArray<T> src, T[] dst)
        {
            Copy(src, 0, dst, 0, src.Length);
        }
        public static void Copy(NativeArray<T> src, T[] dst, int length)
        {
            Copy(src, 0, dst, 0, length);
        }
        public static void Copy(NativeArray<T> src, int srcIndex, T[] dst, int dstIndex, int length)
        {
            UDebug.Assert(src.IsCreated);
            UDebug.Assert(src.Length >= srcIndex + length);
            UDebug.Assert(dst != null);
            UDebug.Assert(dst.Length >= dstIndex + length);

            for (int i = 0; i < length; i++)
                dst[i] = src[i];
        }

        public static void Copy(T[] src, NativeArray<T> dst)
        {
            Copy(src, 0, dst, 0, src.Length);
        }
        public static void Copy(T[] src, NativeArray<T> dst, int length)
        {
            Copy(src, 0, dst, 0, length);
        }
        public static void Copy(T[] src, int srcIndex, NativeArray<T> dst, int dstIndex, int length)
        {
            UDebug.Assert(src != null);
            UDebug.Assert(src.Length >= srcIndex + length);
            UDebug.Assert(dst.IsCreated);
            UDebug.Assert(dst.Length >= dstIndex + length);

            for (int i = 0; i < length; i++)
                dst[i] = src[i];
        }

#if UNITY
        [WriteAccessRequired]
#endif
        public void CopyFrom(T[] array)
        {
            Copy(array, 0, this, 0, array.Length);
        }
#if UNITY
        [WriteAccessRequired]
#endif
        public void CopyFrom(NativeArray<T> array)
        {
            Copy(array, this);
        }
        public void CopyTo(T[] array)
        {
            Copy(this, array, Length);
        }
        public void CopyTo(NativeArray<T> array)
        {
            Copy(this, array);
        }


        public T[] ToArray()
        {
            var arr = new T[Length];

            Copy(this, 0, arr, 0, arr.Length);

            return arr;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return UnsafeArray.GetIterator<T>(m_inner);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

#if UNITY
        [WriteAccessRequired]
#endif
        public void Dispose()
        {
            UnsafeArray.Free(m_inner);
            m_inner = null;
        }
    }
}
