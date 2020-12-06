using System;

namespace UnsafeCollections
{
    internal sealed class AssertException : Exception
    {
        public AssertException()
        { }

        public AssertException(string message) : base(message)
        { }
    }
}
