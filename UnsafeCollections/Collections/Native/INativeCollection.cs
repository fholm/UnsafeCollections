using System;
using System.Collections.Generic;
using System.Text;

namespace UnsafeCollections.Collections.Native
{
    public interface INativeCollection<T> where T : unmanaged
    {
        /// <summary>
        /// The number of items in the collection
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Copies a collection into an array.
        /// </summary>
        /// <param name="array">The destination array</param>
        /// <param name="index">The index of the array to copy to</param>
        void CopyTo(Array array, int index);
    }
}
