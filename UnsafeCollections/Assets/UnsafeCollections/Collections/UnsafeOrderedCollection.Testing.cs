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
#define UNSAFE_COLLECTIONS_TESTING

#if UNSAFE_COLLECTIONS_TESTING
using System;
using System.Collections.Generic;

namespace Collections.Unsafe {
  unsafe partial struct UnsafeOrderedCollection {
    internal static UnsafeOrderedCollection* Allocate<T>(int capacity)
      where T : unmanaged, IComparable<T> {
      return Allocate(capacity, sizeof(T));
    }

    internal static UnsafeOrderedCollection* Allocate(int capacity, int valStride) {
      var entryStride  = sizeof(Entry);
      var valAlignment = Native.GetAlignment(valStride);

      // alignment we need is max of the two alignments
      var alignment = Math.Max(Entry.ALIGNMENT, valAlignment);

      // calculate strides for all elements
      valStride   = Native.RoundToAlignment(valStride,   alignment);
      entryStride = Native.RoundToAlignment(entryStride, alignment);

      // allocate dict, buckets and entries buffer separately
      var collection = Native.MallocAndClear<UnsafeOrderedCollection>();

      // init dynamic buffer
      UnsafeBuffer.InitDynamic(&collection->Entries, capacity, entryStride + valStride);

      collection->FreeCount = 0;
      collection->UsedCount = 0;
      collection->KeyOffset = entryStride;

      return collection;
    }

    internal static void Free(UnsafeOrderedCollection* collection) {
      // free the buffer
      UnsafeBuffer.Free(&collection->Entries);

      // free collection header itself
      Native.Free(collection);
    }

    public static string PrintTree<T>(UnsafeOrderedCollection* collection, Func<T, string> print) where T : unmanaged {
      return PrintEntry<T>(collection, collection->Root, print);
    }

    static string PrintEntry<T>(UnsafeOrderedCollection* collection, int index, Func<T, string> print) where T : unmanaged {
      var entry = GetEntry(collection, index);
      if (entry == null) {
        return "*";
      }

      if (entry->Left == 0 && entry->Right == 0) {
        return GetKey<T>(collection, index).ToString();
      }

      var key   = print(GetKey<T>(collection, index));
      var bal   = PrintBalance(entry);
      var left  = PrintEntry<T>(collection, entry->Left,  print);
      var right = PrintEntry<T>(collection, entry->Right, print);

      if (index == collection->Root) {
        return $"{key}{bal}={left}|{right}";
      } else {
        return $"[{key}{bal}={left}|{right}]";
      }
    }

    static string PrintBalance(Entry* entry) {
      switch (entry->Balance) {
        case -2: return "RR";
        case -1: return "R";
        case 0:  return "";
        case +1: return "L";
        case +2: return "LL";
      }

      throw new InvalidOperationException(entry->Balance.ToString());
    }

    public static void VisitNodes<T>(UnsafeOrderedCollection* collection, Action<T?, T, int, bool> callback) where T : unmanaged {
      Stack<(int, int, int, bool)> entries = new Stack<(int, int, int, bool)>();

      entries.Push((0, collection->Root, 0, false));

      while (entries.Count > 0) {
        var (p, n, d, s) = entries.Pop();

        if (n > 0) {
          var ne = GetEntry(collection, n);

          entries.Push((n, ne->Left, d + 1, true));
          entries.Push((n, ne->Right, d + 1, false));

          if (p > 0) {
            callback(GetKey<T>(collection, p), GetKey<T>(collection, n), d, s);
          } else {
            callback(null, GetKey<T>(collection, n), d, s);
          }
        }
      }
    }
  }
}
#endif