using System;
using System.Diagnostics;
using UnsafeCollections.Collections.Native;

namespace UnsafeCollections.Debug.TypeProxies
{
    internal struct NativeArrayDebugView<T> where T : unmanaged
    {
        private readonly INativeArray<T> m_array;

        public NativeArrayDebugView(INativeArray<T> array)
        {
            if (array == null || !array.IsCreated)
                throw new ArgumentNullException(nameof(array));

            m_array = array;
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items => m_array.ToArray();
    }
}
