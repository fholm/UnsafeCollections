namespace UnsafeCollections.Collections.Native
{
    internal interface INativeArray<T> where T : unmanaged
    {
        /// <summary>
        /// Returns 'True' if the underlying buffer is allocated.
        /// </summary>
        bool IsCreated { get; }

        /// <summary>
        /// The number of items in the collection
        /// </summary>
        int Length { get; }

        /// <summary>
        /// Copies a collection into an array.
        /// </summary>
        T[] ToArray();
    }
}
