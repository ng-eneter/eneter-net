/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/

#if !COMPACT_FRAMEWORK

using System;
using System.Collections.Generic;
using System.Threading;

namespace Eneter.Messaging.Diagnostic
{
    internal class ThreadLock : IDisposable
    {
        public static ThreadLock Lock(object obj)
        {
            if (IsHeldByCurrentThread(obj))
            {
                return null;
            }

            return new ThreadLock(obj);
        }

        private ThreadLock(object obj)
        {
            myObj = obj;

            int aStartAcquiringTime = Environment.TickCount;

            // Wait until the lock is acquired.
            Monitor.Enter(obj);

            lock (myLocks)
            {
                myLocks[obj] = Thread.CurrentThread;
            }

            int aStopAcquiringTime = Environment.TickCount;
            int anElapsedTime = aStartAcquiringTime - aStartAcquiringTime;

            if (EneterTrace.DetailLevel == EneterTrace.EDetailLevel.Debug)
            {
                string[] aMessage = { "LOCKED ", anElapsedTime.ToString() };
                EneterTrace.Debug(1, string.Join("", aMessage));
            }

            if (anElapsedTime >= 1000)
            {
                string[] aMessage = { "Locked after [ms]: ", anElapsedTime.ToString() };
                EneterTrace.Warning(1, string.Join("", aMessage));
            }

            myLockTime = aStopAcquiringTime;
        }

        void IDisposable.Dispose()
        {
            lock (myLocks)
            {
                myLocks.Remove(myObj);
            }

            // Release the lock.
            Monitor.Exit(myObj);
            

            int anElapsedTime = Environment.TickCount - myLockTime;

            if (EneterTrace.DetailLevel == EneterTrace.EDetailLevel.Debug)
            {
                string[] aMessage = { "UNLOCKED ", anElapsedTime.ToString() };
                EneterTrace.Debug(1, string.Join("", aMessage));
            }
            if (anElapsedTime >= 1000)
            {
                string[] aMessage = { "Unlocked after [ms]: ", anElapsedTime.ToString() };
                EneterTrace.Warning(1, string.Join("", aMessage));
            }
        }

        private static bool IsHeldByCurrentThread(object obj)
        {
            lock (myLocks)
            {
                return myLocks.ContainsKey(obj);
            }
        }


        private static Dictionary<object, Thread> myLocks = new Dictionary<object, Thread>();

        private int myLockTime;
        private object myObj;
    }
}

#endif