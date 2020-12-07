using UnsafeCollections.Collections.Native;

namespace UnsafeCollections.Debug.TypeProxies
{
    internal struct NativeArrayDebugView<T> where T : unmanaged
    {
        private readonly NativeArray<T> m_array;

        public NativeArrayDebugView(NativeArray<T> array)
        {
            if (!array.IsCreated)
                throw new System.ArgumentException(nameof(array));

            m_array = array;
        }

        public T[] Items
        {
            get
            {
                return m_array.ToArray();
            }
        }
    }
}
