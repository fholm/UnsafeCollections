using System;
using System.Collections;
using System.Collections.Generic;

namespace Collections.Unsafe {
  public partial struct UnsafeOrderedSet {
    public unsafe struct Iterator<T> : IUnsafeIterator<T> where T : unmanaged {

      int                              _keyOffset;
      UnsafeOrderedCollection.Iterator _iterator;

      public Iterator(UnsafeOrderedSet* set) {
        _keyOffset = set->_collection.KeyOffset;
        _iterator  = new UnsafeOrderedCollection.Iterator(&set->_collection);
      }

      public T Current {
        get {
          if (_iterator.Current == null) {
            throw new InvalidOperationException();
          }
          
          return *(T*)((byte*)_iterator.Current + _keyOffset); 
        }
      }

      object IEnumerator.Current {
        get { return Current; }
      }

      public bool MoveNext() {
        return _iterator.Next();
      }

      public void Reset() {
        _iterator.Reset();
      }

      public void Dispose() {
        
      }

      public IEnumerator<T> GetEnumerator() {
        return this;
      }

      IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
      }
    }

  }
}