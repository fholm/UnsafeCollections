using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using UnsafeCollections.Collections.Unsafe;

namespace UnsafeCollectionsTests.Unsafe
{
    public class UnsafeLockTest
    {
        [Test]
        public void LockUnlockTest()
        {
            UnsafeLock _lock = new UnsafeLock();

            _lock.Lock();

            _lock.Unlock();
        }

        [Test]
        public void UnlockNoLockTest()
        {
            UnsafeLock _lock = new UnsafeLock();

            Assert.Catch<UnsafeLockException>(() => { _lock.Unlock(); });
        }

        //[Test]
        public void ConcurrencyTest()
        {
            ConcurrentObject num = new ConcurrentObject();
            int count = 100000;

            Thread t = new Thread(() =>
            {
                for (int i = 0; i < count; i++)
                {
                    num.Add();
                }
            });
            Thread t2 = new Thread(() =>
            {
                for (int i = 0; i < count; i++)
                {
                    num.Sub();
                }
            });


            t.Start();
            t2.Start();

            t.Join();
            t2.Join();

            Assert.AreEqual(0, num.Num);
        }

        class ConcurrentObject
        {
            private UnsafeLock _lock;

            public int Num
            {
                get; private set;
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public void Add()
            {
                _lock.Lock();
                Num++;
                _lock.Unlock();
            }

            [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
            public void Sub()
            {
                _lock.Lock();
                Num--;
                _lock.Unlock();
            }
        }
    }
}
