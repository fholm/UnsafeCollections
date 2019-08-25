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

namespace Collections.Unsafe {
  public unsafe partial struct UnsafeStack {
    const string STACK_FULL = "Fixed size stack is full";

    UnsafeBuffer _items;
    IntPtr       _typeKey;
    int          _count;

    public static UnsafeStack* Allocate<T>(int capacity, bool fixedSize = false) where T : unmanaged {
      return Allocate(capacity, sizeof(T), fixedSize);
    }

    public static UnsafeStack* Allocate(int capacity, int stride, bool fixedSize = false) {
      Assert.Check(capacity > 0);
      Assert.Check(stride > 0);

      UnsafeStack* stack;

      // fixedSize stack means we are allocating the memory
      // for the stack header and the items in it as one block
      if (fixedSize) {
        var alignment = AllocHelper.GetAlignmentForArrayElement(stride);

        // align stack header size to the elements alignment
        var sizeOfStack = AllocHelper.RoundUpToAlignment(sizeof(UnsafeStack), alignment);
        var sizeOfArray = stride * capacity;

        // allocate memory for stack and array with the correct alignment
        var ptr = AllocHelper.MallocAndClear(sizeOfStack + sizeOfArray, alignment);

        // grab stack ptr
        stack = (UnsafeStack*)ptr;

        // initialize fixed buffer from same block of memory as the stack
        UnsafeBuffer.InitFixed(&stack->_items, (byte*)ptr + sizeOfStack, capacity, stride);
      }

      // dynamic sized stack means we're allocating the stack header
      // and its memory separately
      else {
        // allocate stack separately
        stack = AllocHelper.MallocAndClear<UnsafeStack>();

        // initialize dynamic buffer with separate memory
        UnsafeBuffer.InitDynamic(&stack->_items, capacity, stride);
      }

      // just safety, make sure count is 0
      stack->_count = 0;

      return stack;
    }

    public static void Free(UnsafeStack* stack) {
      Assert.Check(stack != null);

      // if this is a dynamic sized stack, we need to free the buffer by hand
      if (stack->_items.Dynamic) {
        UnsafeBuffer.Free(&stack->_items);
      }

      // clear stack memory just in case
      *stack = default;

      // free stack memory (if this is a fixed size stack, it frees the _items memory also)
      AllocHelper.Free(stack);
    }

    public static int Capacity(UnsafeStack* stack) {
      Assert.Check(stack != null);
      Assert.Check(stack->_items.Ptr != null);
      return stack->_items.Length;
    }

    public static int Count(UnsafeStack* stack) {
      Assert.Check(stack != null);
      Assert.Check(stack->_items.Ptr != null);
      return stack->_count;
    }

    public static void Clear(UnsafeStack* stack) {
      Assert.Check(stack != null);
      Assert.Check(stack->_items.Ptr != null);
      stack->_count = 0;
    }

    public static bool IsFixedSize(UnsafeStack* stack) {
      Assert.Check(stack != null);
      return stack->_items.Dynamic == false;
    }

    public static void Push<T>(UnsafeStack* stack, T item) where T : unmanaged {
      Assert.Check(stack != null);

      var items = stack->_items;
      var count = stack->_count;
      if (count >= items.Length) {
        if (items.Dynamic) {
          Expand(stack);

          // re-assign items after expand
          items = stack->_items;

          // this has to hold now or something went wrong
          Assert.Check(count < items.Length);
        }
        else {
          throw new InvalidOperationException(STACK_FULL);
        }
      }

      // write element
      *(T*)UnsafeBuffer.Element(items.Ptr, count, items.Stride) = item;

      // increment size
      stack->_count = count + 1;
    }

    public static bool TryPop<T>(UnsafeStack* stack, out T item) where T : unmanaged {
      Assert.Check(stack != null);

      var ptr = Peek(stack);
      if (ptr == null) {
        item = default;
        return false;
      }

      // reduce count
      stack->_count = stack->_count - 1;

      // grab out item
      item = *(T*)ptr;
      return true;
    }

    public static bool TryPeek<T>(UnsafeStack* stack, out T item) where T : unmanaged {
      var ptr = Peek(stack);
      if (ptr == null) {
        item = default;
        return false;
      }

      item = *(T*)ptr;
      return true;
    }

    static void* Peek(UnsafeStack* stack) {
      Assert.Check(stack != null);

      var count = stack->_count;
      if (count == 0) {
        return null;
      }

      var items = stack->_items;
      return UnsafeBuffer.Element(items.Ptr, count - 1, items.Stride);
    }

    static void Expand(UnsafeStack* stack) {
      // new buffer for elements
      UnsafeBuffer newItems = default;

      // initialize to double size of existing one
      UnsafeBuffer.InitDynamic(&newItems, stack->_items.Length * 2, stack->_items.Stride);

      // copy memory over from previous items
      UnsafeBuffer.Copy(stack->_items, 0, newItems, 0, stack->_items.Length);

      // free old buffer
      UnsafeBuffer.Free(&stack->_items);

      // replace buffer with new
      stack->_items = newItems;
    }
    
    public static UnsafeList.Iterator<T> GetIterator<T>(UnsafeStack* stack) where T : unmanaged {
      return new UnsafeList.Iterator<T>(stack->_items, 0, stack->_count);
    }
  }
}