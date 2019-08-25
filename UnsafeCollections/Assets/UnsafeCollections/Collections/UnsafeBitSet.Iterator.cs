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
  public partial struct UnsafeBitSet {
    public unsafe struct Iterator : IUnsafeIterator<(int bit, bool set)> {
      UnsafeBitSet* _set;
      int           _current;

      public Iterator(UnsafeBitSet* set) {
        _set     = set;
        _current = -1;
      }

      public bool MoveNext() {
        return ++_current < _set->_sizeBits;
      }

      public void Reset() {
        _current = -1;
      }

      public (int bit, bool set) Current {
        get {
          if ((uint)_current >= (uint)_set->_sizeBits) {
            throw new InvalidOperationException();
          }

          return (_current, IsSet(_set, _current));
        }
      }

      object IEnumerator.Current {
        get { return Current; }
      }

      public void Dispose() {
      }

      public IEnumerator<(int, bool)> GetEnumerator() {
        return this;
      }

      IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
      }
    }
  }
}