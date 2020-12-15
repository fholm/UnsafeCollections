using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnsafeCollections.Collections.Unsafe;
using UnsafeCollections.Debug.TypeProxies;

namespace UnsafeCollections.Collections.Native
{
    [DebuggerDisplay("Count = {Length}")]
    [DebuggerTypeProxy(typeof(NativeCollectionDebugView<>))]
    public unsafe struct NativeQueue<T> : IDisposable, IEnumerable<T>, IEnumerable where T : unmanaged
    {
        private UnsafeQueue* m_inner;


        public UnsafeQueue.Enumerator<T> GetEnumerator()
        {
            return UnsafeQueue.GetEnumerator<T>(m_inner);
        }
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return UnsafeQueue.GetEnumerator<T>(m_inner);
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return UnsafeQueue.GetEnumerator<T>(m_inner);
        }

        public void Dispose()
        {
            UnsafeQueue.Free(m_inner);
            m_inner = null;
        }
    }
}
