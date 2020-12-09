/*
The MIT License (MIT)

Copyright (c) 2019 Fredrik Holmstrom
Copyright (c) 2020 Dennis Corvers

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
using System.Runtime.InteropServices;

namespace UnsafeCollections.Collections.Unsafe
{
    internal unsafe struct UnsafeOrderedCollection
    {
        public const int MAX_DEPTH = 64;
        const string COLLECTION_FULL = "Fixed size ordered collection is full";

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct Entry
        {
            public const int ALIGNMENT = 4;

            public int Left;
            public int Right;
            public int Balance;
        }

        public int Root;
        public int UsedCount;

        public int FreeHead;
        public int FreeCount;

        public int KeyOffset;
        public UnsafeBuffer Entries;

        public static int GetHeight(UnsafeOrderedCollection* collection)
        {
            return Height(collection, collection->Root);
        }

        public static int GetCount(UnsafeOrderedCollection* collection)
        {
            return collection->UsedCount - collection->FreeCount;
        }

        public static void Remove<T>(UnsafeOrderedCollection* collection, T key) where T : unmanaged, IComparable<T>
        {
            collection->Root = DeletePerform<T>(collection, key);
        }

        public static void Insert<T>(UnsafeOrderedCollection* collection, T key) where T : unmanaged, IComparable<T>
        {
            if (collection->FreeCount == 0 && collection->UsedCount == collection->Entries.Length)
            {
                if (collection->Entries.Dynamic == 1)
                {
                    Expand(collection);
                }
                else
                {
                    throw new InvalidOperationException(COLLECTION_FULL);
                }
            }

            collection->Root = InsertPerform<T>(collection, key, false);
        }

        public static Entry* Find<T>(UnsafeOrderedCollection* collection, T key) where T : unmanaged, IComparable<T>
        {
            int entryIndex = collection->Root;

            while (entryIndex != 0)
            {
                var entry = GetEntry(collection, entryIndex);
                var entryKey = GetKey<T>(collection, entryIndex);
                var compare = key.CompareTo(entryKey);
                if (compare < 0)
                {
                    entryIndex = entry->Left;
                }
                else if (compare > 0)
                {
                    entryIndex = entry->Right;
                }
                else
                {
                    return entry;
                }
            }

            return null;
        }

        public static Entry* GetEntry(UnsafeOrderedCollection* collection, int index)
        {
            if (index <= 0)
            {
                return null;
            }
            return collection->Entries.Element<Entry>(index - 1);
        }

        public static T GetKey<T>(UnsafeOrderedCollection* collection, int index) where T : unmanaged
        {
            UDebug.Assert(index > 0);
            var entry = GetEntry(collection, index);
            return *(T*)((byte*)entry + collection->KeyOffset);
        }

        static int Height(UnsafeOrderedCollection* collection, int index)
        {
            if (index == 0)
            {
                return 0;
            }

            var entry = GetEntry(collection, index);
            return 1 + Math.Max(Height(collection, entry->Left), Height(collection, entry->Right));
        }

        static void SetKey<T>(UnsafeOrderedCollection* collection, int index, T value) where T : unmanaged
        {
            var entry = GetEntry(collection, index);
            *(T*)((byte*)entry + collection->KeyOffset) = value;
        }

        static void Expand(UnsafeOrderedCollection* collection)
        {
            UDebug.Assert(collection->Entries.Dynamic == 1);
            UDebug.Assert(collection->FreeCount == 0);
            UDebug.Assert(collection->FreeHead == 0);

            var capacity = collection->Entries.Length * 2;
            var newEntries = default(UnsafeBuffer);

            UnsafeBuffer.InitDynamic(&newEntries, capacity, collection->Entries.Stride);
            UnsafeBuffer.Copy(collection->Entries, 0, newEntries, 0, collection->Entries.Length);

            // free old memory
            UnsafeBuffer.Free(&collection->Entries);

            // new storage
            collection->Entries = newEntries;
        }

        static void FreeEntry(UnsafeOrderedCollection* collection, int entryIndex)
        {
            var entry = GetEntry(collection, entryIndex);

            entry->Right = 0;
            entry->Balance = 0;
            entry->Left = collection->FreeHead;

            collection->FreeHead = entryIndex;
            collection->FreeCount = collection->FreeCount + 1;
        }

        static int CreateEntry<T>(UnsafeOrderedCollection* collection, T key) where T : unmanaged
        {
            Entry* entry;
            int entryIndex;

            if (collection->FreeHead > 0)
            {
                UDebug.Assert(collection->FreeCount > 0);

                // grab from free head
                entry = GetEntry(collection, entryIndex = collection->FreeHead);

                // new free-head is entrys left pointer
                collection->FreeHead = entry->Left;
                collection->FreeCount = collection->FreeCount - 1;

                // we have to clear left pointer
                entry->Left = 0;
            }
            else
            {
                // this has to hold, as Insert<T> checks and expands if needed
                UDebug.Assert(collection->UsedCount < collection->Entries.Length);

                // grab entry from UsedCount, we add increment before as entries are 1 indexed
                entry = GetEntry(collection, entryIndex = ++collection->UsedCount);
            }

            // when we get a new entry, it's left/right/balance all have to be zero
            UDebug.Assert(entry->Left == 0);
            UDebug.Assert(entry->Right == 0);
            UDebug.Assert(entry->Balance == 0);

            // write key to entry
            *(T*)((byte*)entry + collection->KeyOffset) = key;

            // return entry index
            return entryIndex;
        }

        static int InsertPerform<T>(UnsafeOrderedCollection* collection, T insertKey, bool update) where T : unmanaged, IComparable<T>
        {
            const bool L = true;
            const bool R = false;

            int* path = stackalloc int[MAX_DEPTH];
            bool* side = stackalloc bool[MAX_DEPTH];

            // find insertion slot
            int entryIndex = collection->Root;
            int pathSize = 0;

            while (entryIndex != 0 && pathSize < (MAX_DEPTH - 1))
            {
                var entry = GetEntry(collection, entryIndex);
                var entryKey = GetKey<T>(collection, entryIndex);

                var compare = insertKey.CompareTo(entryKey);
                if (compare < 0)
                {
                    path[pathSize] = entryIndex;
                    side[pathSize] = L;
                    entryIndex = entry->Left;
                }
                else if (compare > 0)
                {
                    path[pathSize] = entryIndex;
                    side[pathSize] = R;
                    entryIndex = entry->Right;
                }
                else
                {
                    if (update)
                    {
                        SetKey(collection, entryIndex, insertKey);
                    }

                    return collection->Root;
                }

                ++pathSize;
            }

            // means we hit max depth
            if (entryIndex != 0)
            {
                throw new InvalidOperationException("MAX_DEPTH EXCEEDED");
            }

            // means we're at the root, so just insert and return it
            if (pathSize == 0)
            {
                return CreateEntry<T>(collection, insertKey);
            }

            // now we've created the entry at entryIndex,
            // insert it into path to simplify the reblance loop
            path[pathSize++] = CreateEntry<T>(collection, insertKey);

            // we initialize i to pathSize-2 because we don't
            // have to re-balance or check the node we just
            // inserted, this also simplifies the 
            var i = pathSize - 2;
            var b = 0;

            // go back up path
            for (; i >= 0; --i)
            {
                var entry = GetEntry(collection, path[i]);

                // patch up left/right entry from previous path step
                if (side[i])
                {
                    entry->Left = path[i + 1];
                    entry->Balance += 1;
                }
                else
                {
                    entry->Right = path[i + 1];
                    entry->Balance -= 1;
                }

                // if we hit a 0 balance, we *MUST* stop 
                // or otherwise we'll adjust balances
                // incorrectly on the way back up
                if (entry->Balance == 0)
                {
                    break;
                }

                // case 1 : left heavy
                if (entry->Balance == 2)
                {
                    // case 1-1 : left subtree is also left heavy
                    var left = GetEntry(collection, entry->Left);
                    if (left->Balance == 1)
                    {
                        path[i] = RotateRight(collection, path[i], &b);
                    }

                    // case 1-2 : left subtree is right heavy 
                    else
                    {
                        path[i] = RotateLeftRight(collection, path[i]);
                    }

                    break;
                }

                // case 2 : right heavy 
                else if (entry->Balance == -2)
                {
                    // case 2-1 : right subtree is also right heayv
                    var right = GetEntry(collection, entry->Right);
                    if (right->Balance == -1)
                    {
                        path[i] = RotateLeft(collection, path[i], &b);
                    }

                    // case 2-2 : right subtree is left heavy
                    else
                    {
                        path[i] = RotateRightLeft(collection, path[i]);
                    }

                    break;
                }
            }

            // this is needed to patch up rotation result
            // to it's parent, since we break out of the loop
            if (--i >= 0)
            {
                var entry = GetEntry(collection, path[i]);
                if (side[i])
                {
                    entry->Left = path[i + 1];
                }
                else
                {
                    entry->Right = path[i + 1];
                }
            }

            // path 0 is our root
            return path[0];
        }

        static int DeletePerform<T>(UnsafeOrderedCollection* collection, T deleteKey) where T : unmanaged, IComparable<T>
        {
            const int L = -1;
            const int R = +1;

            int* path = stackalloc int[MAX_DEPTH];
            sbyte* side = stackalloc sbyte[MAX_DEPTH];

            int entryIndex = collection->Root;
            int pathSize = 0;

            while (entryIndex != 0 && pathSize < (MAX_DEPTH - 1))
            {
                var entry = GetEntry(collection, entryIndex);
                var entryKey = GetKey<T>(collection, entryIndex);

                var compare = deleteKey.CompareTo(entryKey);
                if (compare < 0)
                {
                    path[pathSize] = entryIndex;
                    side[pathSize] = L;
                    entryIndex = entry->Left;
                    ++pathSize;
                }
                else if (compare > 0)
                {
                    path[pathSize] = entryIndex;
                    side[pathSize] = R;
                    entryIndex = entry->Right;
                    ++pathSize;
                }
                else
                {
                    path[pathSize] = 0;
                    side[pathSize] = 0;
                    ++pathSize;
                    break;
                }
            }

            // could not find entry
            if (entryIndex == 0)
            {
                return collection->Root;
            }

            // so... delete handling is annoying as hell, there's a
            // lot of optimizations that can be done here,
            // tried to get as many in as I could come up with 

            int entryLeft;
            int entryRight;
            int entryBalance;
            int entryPathIndex = pathSize - 1;

            {
                var entry = GetEntry(collection, entryIndex);
                entryLeft = entry->Left;
                entryRight = entry->Right;
                entryBalance = entry->Balance;
                FreeEntry(collection, entryIndex);
            }

            // case 1 : no children
            if (entryLeft + entryRight == 0)
            {
                // we are at root with no children, special case
                if (pathSize == 1)
                {
                    return 0;
                }
            }

            // case 2 : only left child
            if (entryRight == 0)
            {
                path[entryPathIndex] = entryLeft;
            }

            // case 3 : only right child
            else if (entryLeft == 0)
            {
                path[entryPathIndex] = entryRight;
            }

            // case 4 : left and right child
            else
            {
                var successorParentFound = false;
                var successorIndex = entryRight;
                var successor = GetEntry(collection, successorIndex);

                // find in-order successor, which is
                // the left most entry of the right subtree
                while (successor->Left != 0)
                {
                    // keep going down the left of entrys right subtree 
                    path[pathSize] = successorIndex;
                    side[pathSize] = L;
                    ++pathSize;

                    successorParentFound = true;
                    successorIndex = successor->Left;
                    successor = GetEntry(collection, successorIndex);
                }

                // replace entry with successor, we went down
                // right sub-tree to find it so side is R
                path[entryPathIndex] = successorIndex;
                side[entryPathIndex] = R;

                // successor gets balance and left tree from entry, 
                // we need to explicitly assign the left tree
                // because it will not be done when go back 
                // and patch up the tree while we balance it
                successor->Left = entryLeft;
                successor->Balance = entryBalance;

                // successor was not the direct right entry,
                // so needed to go down left path of right
                // sub-tree, we need to insert successor's old right
                // to attach it to the parent of the successor
                if (successorParentFound)
                {
                    path[pathSize] = successor->Right;
                    side[pathSize] = 0;
                    ++pathSize;
                }
            }

            int i = pathSize - 1;
            int m = i;
            var b = 0;

            if (path[i] == 0)
            {
                --i;
            }

            for (; i >= 0; --i)
            {
                var e = GetEntry(collection, path[i]);

                if (i < m)
                {
                    switch (side[i])
                    {
                        case L:
                            e->Left = path[i + 1];
                            break;

                        case R:
                            e->Right = path[i + 1];
                            break;
                    }
                }

                e->Balance += side[i];

                // case 1: left heavy
                if (e->Balance == 2)
                {
                    // case 1-1 : left left, entry is left heavy and it's
                    // left subtree is neutral or left heavy 
                    var left = GetEntry(collection, e->Left);
                    if (left->Balance >= 0)
                    {
                        path[i] = RotateRight(collection, path[i], &b);

                        if (b == -1)
                        {
                            break;
                        }
                    }
                    // case 1-2: left right, entry is left heavy, and it's
                    // left tree is right heavy
                    else
                    {
                        path[i] = RotateLeftRight(collection, path[i]);
                    }
                }

                // case 2: right heavy
                else if (e->Balance == -2)
                {
                    // case 2-1: right right, entry is right heavy and it's
                    // right subtree is neutral or right heavy
                    var right = GetEntry(collection, e->Right);
                    if (right->Balance <= 0)
                    {
                        path[i] = RotateLeft(collection, path[i], &b);

                        if (b == 1)
                        {
                            break;
                        }
                    }
                    // case 2-2: right left, entry is right heavy and it's
                    // right subtree is left heavy
                    else
                    {
                        path[i] = RotateRightLeft(collection, path[i]);
                    }
                }
                else if (e->Balance != 0)
                {
                    break;
                }
            }

            // patch up all paths all the way to root
            for (--i; i >= 0; --i)
            {
                var e = GetEntry(collection, path[i]);
                switch (side[i])
                {
                    case L:
                        e->Left = path[i + 1];
                        break;
                    case R:
                        e->Right = path[i + 1];
                        break;
                }
            }

            return path[0];
        }

        static int RotateLeftRight(UnsafeOrderedCollection* collection, int nodeIndex)
        {
            var node = GetEntry(collection, nodeIndex);

            // left
            var leftIndex = node->Left;
            var left = GetEntry(collection, leftIndex);

            // left right
            var leftRightIndex = left->Right;
            var leftRight = GetEntry(collection, leftRightIndex);

            // left right left
            var leftRightLeftIndex = leftRight->Left;

            // left right right
            var leftRightRightIndex = leftRight->Right;

            node->Left = leftRightRightIndex;
            left->Right = leftRightLeftIndex;

            leftRight->Left = leftIndex;
            leftRight->Right = nodeIndex;

            if (leftRight->Balance == -1)
            {
                node->Balance = 0;
                left->Balance = 1;
            }
            else if (leftRight->Balance == 0)
            {
                node->Balance = 0;
                left->Balance = 0;
            }
            else
            {
                node->Balance = -1;
                left->Balance = 0;
            }

            // balance is ALWAYS zero after a left/right rotation
            leftRight->Balance = 0;

            return leftRightIndex;
        }

        static int RotateRightLeft(UnsafeOrderedCollection* collection, int entryIndex)
        {
            var node = GetEntry(collection, entryIndex);

            // right
            var rightIndex = node->Right;
            var right = GetEntry(collection, rightIndex);

            // right left
            var rightLeftIndex = right->Left;
            var rightLeft = GetEntry(collection, rightLeftIndex);

            // right left left
            var rightLeftLeftIndex = rightLeft->Left;

            // right left right
            var rightLeftRightIndex = rightLeft->Right;

            node->Right = rightLeftLeftIndex;
            right->Left = rightLeftRightIndex;

            rightLeft->Right = rightIndex;
            rightLeft->Left = entryIndex;

            if (rightLeft->Balance == 1)
            {
                node->Balance = 0;
                right->Balance = -1;
            }
            else if (rightLeft->Balance == 0)
            {
                node->Balance = 0;
                right->Balance = 0;
            }
            else
            {
                node->Balance = 1;
                right->Balance = 0;
            }

            // balance is ALWAYS zero after a right/left rotation
            rightLeft->Balance = 0;

            return rightLeftIndex;
        }

        static int RotateRight(UnsafeOrderedCollection* collection, int entryIndex, int* balance)
        {
            var entry = GetEntry(collection, entryIndex);

            // grab left 
            var leftIndex = entry->Left;
            var left = GetEntry(collection, leftIndex);

            // new left of entry is left->right
            entry->Left = left->Right;

            // new right of left is the entry
            left->Right = entryIndex;

            // reduce left balance by 1
            *balance = --left->Balance;

            // new entry balance becomes inverse of it's previous lefts balance
            entry->Balance = -left->Balance;

            // return entries old left to take its place in the path
            return leftIndex;
        }

        static int RotateLeft(UnsafeOrderedCollection* collection, int entryIndex, int* balance)
        {
            var entry = GetEntry(collection, entryIndex);

            // grab right
            var rightIndex = entry->Right;
            var right = GetEntry(collection, rightIndex);

            // new right of entry is right->left
            entry->Right = right->Left;

            //new left of right is the entry
            right->Left = entryIndex;

            // increase right balance by 1
            *balance = ++right->Balance;

            // balance of entry is inverse of old rights balance
            entry->Balance = -right->Balance;

            // return right index
            return rightIndex;
        }


        public unsafe struct Enumerator
        {
#pragma warning disable IDE0044
            fixed int _stack[UnsafeOrderedCollection.MAX_DEPTH];
#pragma warning restore IDE0044

            int _depth;
            int _index;

            public Entry* Current;
            public UnsafeOrderedCollection* Collection;

            public Enumerator(UnsafeOrderedCollection* collection)
            {
                Collection = collection;
                Current = null;

                _depth = 0;
                _index = Collection->Root;
            }

            public bool MoveNext()
            {
                if (Current != null)
                {
                    _index = Current->Right;
                }

                while (_index != 0 || _depth > 0)
                {
                    // push current left-most on stack
                    while (_index != 0)
                    {
                        // check for max depth
                        UDebug.Assert(_depth < MAX_DEPTH);

                        // pushes current on stack
                        _stack[_depth++] = _index;

                        // grab next left
                        _index = GetEntry(Collection, _index)->Left;
                    }

                    // grab from stack
                    _index = _stack[--_depth];
                    Current = GetEntry(Collection, _index);
                    return true;
                }

                Current = null;
                return false;
            }

            public void Reset()
            {
                _depth = 0;
                _index = Collection->Root;
            }
        }
    }
}