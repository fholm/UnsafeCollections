using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using UnsafeCollections.Collections.Native;

namespace UnsafeCollections.Debug.TypeProxies
{
    internal struct NativeCollectionDebugView<T> where T : unmanaged
    {
        private readonly INativeCollection<T> m_collection;

        public NativeCollectionDebugView(INativeCollection<T> collection)
        {
            m_collection = collection ?? throw new ArgumentNullException(nameof(collection));
        }

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public T[] Items
        {
            get
            {
                T[] items = new T[m_collection.Count];
                m_collection.CopyTo(items, 0);
                return items;
            }
        }
    }
}
