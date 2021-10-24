/*
The MIT License (MIT)

Copyright (c) 2019 Fredrik Holmstrom

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

using System;
using System.Collections;
using System.Collections.Generic;

namespace Collections.Unsafe {
  public partial struct UnsafeArray {
    // this is basically identical to the list iterator, but because array
    // uses a different structure internally it needs its own implementation
    public unsafe struct Iterator<T> : IUnsafeIterator<T> where T : unmanaged {
      T*           _current;
      int          _index;
      UnsafeArray* _array;

      internal Iterator(UnsafeArray* array) {
        _index   = -1;
        _array   = array;
        _current = null;
      }

      public bool MoveNext() {
        if (++_index < _array->_length) {
          _current = GetPtr<T>(_array, _index);
          return true;
        }

        _current = null;
        return false;
      }

      public void Reset() {
        _index   = -1;
        _current = null;
      }

      public T Current {
        get {
          if (_current == null) {
            throw new InvalidOperationException();
          }

          return *_current;
        }
      }

      object IEnumerator.Current {
        get { return Current; }
      }

      public void Dispose() {
      }

      public Iterator<T> GetEnumerator() {
        return this;
      }

      IEnumerator<T> IEnumerable<T>.GetEnumerator() {
        return this;
      }

      IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
      }
    }
  }
}