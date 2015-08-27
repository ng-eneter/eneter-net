/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/

using System;
using System.Collections.Generic;
using System.Threading;

namespace Eneter.Messaging.Diagnostic
{
    internal class ThreadLock : IDisposable
    {
        private ThreadLock(object obj)
        {
            myObj = obj;
        }

        public static ThreadLock Lock(object obj)
        {
            ThreadLock aThreadLock;
            lock (myActiveLocks)
            {
                myActiveLocks.TryGetValue(obj, out aThreadLock);

                if (aThreadLock == null)
                {
                    aThreadLock = new ThreadLock(obj);
                    myActiveLocks[obj] = aThreadLock;
                }
                else if (aThreadLock.OwningThread == Thread.CurrentThread)
                {
                    // If it is re-entering the lock.
                    return null;
                }

                ++aThreadLock.ReferenceCounter;
            }


            aThreadLock.Lock();

            return aThreadLock;
        }

        void IDisposable.Dispose()
        {
            Unlock();

            lock (myActiveLocks)
            {
                --ReferenceCounter;
                if (ReferenceCounter == 0)
                {
                    myActiveLocks.Remove(myObj);
                }
            }
        }

        private void Lock()
        {
            DateTime aStartAcquiringTime = DateTime.Now;

            // Wait until the lock is acquired.
            Monitor.Enter(myObj);

            DateTime aStopAcquiringTime = DateTime.Now;
            TimeSpan anElapsedTime = aStartAcquiringTime - aStartAcquiringTime;

            OwningThread = Thread.CurrentThread;

            if (EneterTrace.DetailLevel == EneterTrace.EDetailLevel.Debug)
            {
                string[] aMessage = { "LOCKED ", anElapsedTime.ToString() };
                EneterTrace.Debug(2, string.Join("", aMessage));
            }

            if (anElapsedTime >= TimeSpan.FromMilliseconds(1000))
            {
                string[] aMessage = { "Locked after [ms]: ", anElapsedTime.TotalMilliseconds.ToString() };
                EneterTrace.Warning(2, string.Join("", aMessage));
            }

            myLockTime = aStopAcquiringTime;
        }

        private void Unlock()
        {
            OwningThread = null;

            // Release the lock.
            Monitor.Exit(myObj);

            TimeSpan anElapsedTime = DateTime.Now - myLockTime;

            if (EneterTrace.DetailLevel == EneterTrace.EDetailLevel.Debug)
            {
                string[] aMessage = { "UNLOCKED ", anElapsedTime.ToString() };
                EneterTrace.Debug(2, string.Join("", aMessage));
            }
            if (anElapsedTime >= TimeSpan.FromMilliseconds(1000))
            {
                string[] aMessage = { "Unlocked after [ms]: ", anElapsedTime.TotalMilliseconds.ToString() };
                EneterTrace.Warning(2, string.Join("", aMessage));
            }
        }

        private Thread OwningThread { get; set; }
        private int ReferenceCounter { get; set; }

        private DateTime myLockTime;
        private object myObj;

        private static Dictionary<object, ThreadLock> myActiveLocks = new Dictionary<object, ThreadLock>();
    }
}
