using UnsafeCollections.Collections.Native;

namespace UnsafeCollections.Debug.TypeProxies
{
    internal sealed class NativeArrayDebugView<T> where T : unmanaged
    {
        private readonly NativeArray<T> m_array;

        public NativeArrayDebugView(NativeArray<T> array)
        {
            m_array = array;
        }

        public T[] Items
        {
            get
            {
                if (!m_array.IsCreated)
                    throw new System.NullReferenceException();

                return m_array.ToArray();
            }
        }
    }
}
