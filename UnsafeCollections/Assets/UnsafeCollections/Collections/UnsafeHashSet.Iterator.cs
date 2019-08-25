using System;
using System.Collections;
using System.Collections.Generic;

namespace Collections.Unsafe {
  public partial struct UnsafeHashSet {
    
    public unsafe struct Iterator<T> : IUnsafeIterator<T> where T : unmanaged {
      UnsafeHashCollection.Iterator _iterator;
      int                           _keyOffset;

      public Iterator(UnsafeHashSet* set) {
        _keyOffset = set->_collection.KeyOffset;
        _iterator  = new UnsafeHashCollection.Iterator(&set->_collection);
      }

      public bool MoveNext() {
        return _iterator.Next();
      }

      public void Reset() {
        _iterator.Reset();
      }

      object IEnumerator.Current {
        get { return Current; }
      }

      public T Current {
        get {
          if (_iterator.Current == null) {
            throw new InvalidOperationException();
          }

          return *(T*)((byte*)_iterator.Current + _keyOffset);
        }
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