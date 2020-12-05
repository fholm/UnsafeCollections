using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace UnsafeCollections.Collections
{
    public unsafe struct ListIterator<T> : IUnsafeIterator<T> where T : unmanaged
    {
        T* _current;
        int _index;
        readonly int _count;
        readonly int _offset;
        UnsafeBuffer _buffer;

        internal ListIterator(UnsafeBuffer buffer, int offset, int count)
        {
            _index = -1;
            _count = count;
            _offset = offset;
            _buffer = buffer;
            _current = null;
        }

        public bool MoveNext()
        {
            if (++_index < _count)
            {
                _current = (T*)UnsafeBuffer.Element(_buffer.Ptr, (_offset + _index) % _buffer.Length, _buffer.Stride);
                return true;
            }

            _current = null;
            return false;
        }

        public void Reset()
        {
            _index = -1;
            _current = null;
        }

        public T Current
        {
            get
            {
                if (_current == null)
                {
                    throw new InvalidOperationException();
                }

                return *_current;
            }
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public void Dispose()
        {
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
