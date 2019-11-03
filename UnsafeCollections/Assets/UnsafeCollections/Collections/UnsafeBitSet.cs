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
using Unity.Collections.LowLevel.Unsafe;

namespace Collections.Unsafe {
  public unsafe partial struct UnsafeBitSet {
    const int   WORD_SIZE      = sizeof(ulong);
    const int   WORD_SIZE_BITS = WORD_SIZE * 8;
    const ulong WORD_ONE       = 1UL;
    const ulong WORD_ZERO      = 0UL;

    const string SET_DIFFERENT_SIZE      = "Sets have different size";
    const string SET_SIZE_LESS_THAN_ONE  = "Set size can't be less than 1";
    const string SET_ARRAY_LESS_CAPACITY = "Array is not long enough to hold all bits";

    [NativeDisableUnsafePtrRestriction]
    ulong* _bits;

    int _sizeBits;
    int _sizeBuckets;

    public static UnsafeBitSet* Alloc(int size) {
      if (size < 1) {
        throw new InvalidOperationException(SET_SIZE_LESS_THAN_ONE);
      }

      // round to WORD_BIT_SIZE alignment, as we operate on the bitset using WORD_SIZE
      size = AllocHelper.RoundUpToAlignment(size, WORD_SIZE_BITS);

      var sizeOfHeader = AllocHelper.RoundUpToAlignment(sizeof(UnsafeBitSet), WORD_SIZE);
      var sizeOfBuffer = size / 8; // 8 bits per byte

      var ptr = AllocHelper.MallocAndClear(sizeOfHeader + sizeOfBuffer);
      var set = (UnsafeBitSet*)ptr;

      // set bit capacity
      set->_sizeBits    = size;
      set->_sizeBuckets = size / WORD_SIZE_BITS;
      set->_bits        = (ulong*)((byte*)ptr + sizeOfHeader);

      return set;
    }

    public static void Free(UnsafeBitSet* set) {
      // clear memory
      *set = default;

      // free memory
      AllocHelper.Free(set);
    }

    public static int Size(UnsafeBitSet* set) {
      return set->_sizeBits;
    }

    public static void Clear(UnsafeBitSet* set) {
      AllocHelper.MemClear(set->_bits, set->_sizeBuckets * WORD_SIZE);
    }

    public static void Set(UnsafeBitSet* set, int bit) {
      if ((uint)bit >= (uint)set->_sizeBits) {
        throw new IndexOutOfRangeException();
      }

      set->_bits[bit / WORD_SIZE_BITS] |= WORD_ONE << (bit % WORD_SIZE_BITS);
    }

    public static void Clear(UnsafeBitSet* set, int bit) {
      if ((uint)bit >= (uint)set->_sizeBits) {
        throw new IndexOutOfRangeException();
      }

      set->_bits[bit / WORD_SIZE_BITS] &= ~(WORD_ONE << (bit % WORD_SIZE_BITS));
    }

    public static bool IsSet(UnsafeBitSet* set, int bit) {
      if ((uint)bit >= (uint)set->_sizeBits) {
        throw new IndexOutOfRangeException();
      }

      return (set->_bits[bit / WORD_SIZE_BITS] & (WORD_ONE << (bit % WORD_SIZE_BITS))) != WORD_ZERO;
    }

    public static void Or(UnsafeBitSet* set, UnsafeBitSet* other) {
      if (set->_sizeBits != other->_sizeBits) {
        throw new InvalidOperationException(SET_DIFFERENT_SIZE);
      }

      for (var i = (set->_sizeBuckets - 1); i >= 0; --i) {
        set->_bits[i] |= other->_bits[i];
      }
    }

    public static void And(UnsafeBitSet* set, UnsafeBitSet* other) {
      if (set->_sizeBits != other->_sizeBits) {
        throw new InvalidOperationException(SET_DIFFERENT_SIZE);
      }

      for (var i = (set->_sizeBuckets - 1); i >= 0; --i) {
        set->_bits[i] &= other->_bits[i];
      }
    }

    public static void Xor(UnsafeBitSet* set, UnsafeBitSet* other) {
      if (set->_sizeBits != other->_sizeBits) {
        throw new InvalidOperationException(SET_DIFFERENT_SIZE);
      }

      for (var i = (set->_sizeBuckets - 1); i >= 0; --i) {
        set->_bits[i] ^= other->_bits[i];
      }
    }

    public static bool AnySet(UnsafeBitSet* set) {
      for (var i = (set->_sizeBuckets - 1); i >= 0; --i) {
        if (set->_bits[i] != WORD_ZERO) {
          return true;
        }
      }

      return false;
    }

    public static Iterator GetIterator(UnsafeBitSet* set) {
      return new Iterator(set);
    }

    public static int GetSetBits(UnsafeBitSet* set, UnsafeArray* array) {
      Assert.Check(UnsafeArray.GetTypeHandle(array) == typeof(int).TypeHandle.Value);

      if (UnsafeArray.Length(array) < set->_sizeBits) {
        throw new InvalidOperationException(SET_ARRAY_LESS_CAPACITY);
      }

      var setCount    = 0;
      var bitOffset   = 0;
      var arrayBuffer = (int*)UnsafeArray.GetBuffer(array);

      for (var i = 0; i < set->_sizeBuckets; ++i) {
        var word64 = set->_bits[i];
        if (word64 == WORD_ZERO) {
          // since we're skipping whole word, step up offset 
          bitOffset += WORD_SIZE_BITS;
          continue;
        }

        var word32Count = 0;

        NEXT_WORD32:
        var word32 = *((uint*)&word64 + word32Count);
        if (word32 != 0) {
          var word16Count = 0;

          NEXT_WORD16:
          var word16 = *((ushort*)&word32 + word16Count);
          if (word16 != 0) {
            var word8Count = 0;

            NEXT_WORD8:
            var word8 = *((byte*)&word16 + word8Count);
            if (word8 != 0) {
              if ((word8 & (1 << 0)) == 1 << 0) arrayBuffer[setCount++] = (bitOffset + 0);
              if ((word8 & (1 << 1)) == 1 << 1) arrayBuffer[setCount++] = (bitOffset + 1);
              if ((word8 & (1 << 2)) == 1 << 2) arrayBuffer[setCount++] = (bitOffset + 2);
              if ((word8 & (1 << 3)) == 1 << 3) arrayBuffer[setCount++] = (bitOffset + 3);
              if ((word8 & (1 << 4)) == 1 << 4) arrayBuffer[setCount++] = (bitOffset + 4);
              if ((word8 & (1 << 5)) == 1 << 5) arrayBuffer[setCount++] = (bitOffset + 5);
              if ((word8 & (1 << 6)) == 1 << 6) arrayBuffer[setCount++] = (bitOffset + 6);
              if ((word8 & (1 << 7)) == 1 << 7) arrayBuffer[setCount++] = (bitOffset + 7);
            }

            // always step up bitoffset here
            bitOffset += (WORD_SIZE_BITS / 8);

            if (word8Count == 0) {
              ++word8Count;

              // go back
              goto NEXT_WORD8;
            }
          }
          else {
            bitOffset += (WORD_SIZE_BITS / 4);
          }

          if (word16Count == 0) {
            ++word16Count;

            // go back
            goto NEXT_WORD16;
          }
        }
        else {
          bitOffset += (WORD_SIZE_BITS / 2);
        }

        if (word32Count == 0) {
          ++word32Count;

          // go back
          goto NEXT_WORD32;
        }
      }

      return setCount;
    }
  }
}