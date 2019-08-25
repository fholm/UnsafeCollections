using System;
using System.Collections;
using System.Collections.Generic;

namespace Collections.Unsafe {
  public partial struct UnsafeHashMap {
    
    public unsafe struct Iterator<K, V> : IUnsafeIterator<(K key, V value)>
      where K : unmanaged
      where V : unmanaged {

      UnsafeHashCollection.Iterator _iterator;
      int                           _keyOffset;
      int                           _valueOffset;

      public Iterator(UnsafeHashMap* map) {
        _valueOffset = map->_valueOffset;
        _keyOffset   = map->_collection.KeyOffset;
        _iterator    = new UnsafeHashCollection.Iterator(&map->_collection);
      }

      public K CurrentKey {
        get {
          if (_iterator.Current == null) {
            throw new InvalidOperationException();
          }

          return *(K*)((byte*)_iterator.Current + _keyOffset);
        }
      }

      public V CurrentValue {
        get {
          if (_iterator.Current == null) {
            throw new InvalidOperationException();
          }

          return *(V*)((byte*)_iterator.Current + _valueOffset);
        }
      }

      public (K key, V value) Current {
        get { return (CurrentKey, CurrentValue); }
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

      public IEnumerator<(K key, V value) > GetEnumerator() {
        return this;
      }

      IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
      }
    }
  }
}