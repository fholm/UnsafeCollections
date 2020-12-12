using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace UnsafeCollections.Collections.Unsafe
{
    internal class UnsafeLockException : Exception
    {
        public UnsafeLockException()
        { }

        public UnsafeLockException(string message) : base(message)
        { }
    }

    internal struct UnsafeLock
    {
        const int Locked = 1;
        const int Unlocked = 0;

        volatile int _lock;

        public void Lock()
        {
            while (Interlocked.CompareExchange(ref _lock, Locked, Unlocked) != Unlocked)
            {
                Thread.SpinWait(1);
            }
        }

        public void Unlock()
        {
            if (Interlocked.CompareExchange(ref _lock, Unlocked, Locked) != Locked)
            {
                throw new UnsafeLockException();
            }
        }
    }
}
