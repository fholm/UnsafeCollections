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

namespace Collections.Unsafe {
  partial struct UnsafeOrderedCollection {
    public unsafe struct Iterator {
#pragma warning disable 649
      fixed int _stack[MAX_DEPTH];
#pragma warning restore 649

      int _depth;
      int _index;

      public Entry*                   Current;
      public UnsafeOrderedCollection* Collection;

      public Iterator(UnsafeOrderedCollection* collection) {
        Collection = collection;
        Current    = null;

        _depth = 0;
        _index = Collection->Root;
      }

      public bool Next() {
        if (Current != null) {
          _index = Current->Right;
        }

        while (_index != 0 || _depth > 0) {
          // push current left-most on stack
          while (_index != 0) {
            // check for max depth
            Assert.Check(_depth < MAX_DEPTH);

            // pushes current on stack
            _stack[_depth++] = _index;

            // grab next left
            _index = GetEntry(Collection, _index)->Left;
          }

          // grab from stack
          _index  = _stack[--_depth];
          Current = GetEntry(Collection, _index);
          return true;
        }

        Current = null;
        return false;
      }

      public void Reset() {
        _depth = 0;
        _index = Collection->Root;
      }
    }
  }
}