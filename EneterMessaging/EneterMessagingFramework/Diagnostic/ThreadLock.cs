


using System;
using System.Threading;

namespace Eneter.Messaging.Diagnostic
{
    internal class ThreadLock : IDisposable
    {
        public static ThreadLock Lock(object obj)
        {
            return new ThreadLock(obj);
        }

        private ThreadLock(object obj)
        {
            myObj = obj;

            // Wait until the lock is acquired.
            Monitor.Enter(myObj);
        }

        void IDisposable.Dispose()
        {
            // Release the lock.
            Monitor.Exit(myObj);
        }

        private readonly object myObj;
    }
}
