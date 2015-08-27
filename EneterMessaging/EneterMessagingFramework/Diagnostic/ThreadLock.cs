/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Eneter.Messaging.Diagnostic
{
    internal class ThreadLock : IDisposable
    {
        private ThreadLock(object obj)
        {
            if (IsEntered(obj))
            {
                //Monitor.Enter(obj);
                return;
            }

            myObj = obj;
            
            if (EneterTrace.DetailLevel == EneterTrace.EDetailLevel.Debug)
            {
                int aNumberOfWaitings;
                lock (myWaitings)
                {
                    myWaitings.TryGetValue(obj, out aNumberOfWaitings);
                    ++aNumberOfWaitings;
                    myWaitings[obj] = aNumberOfWaitings;
                }

                string[] aMessage = { "LOCKING ", aNumberOfWaitings.ToString() };
                EneterTrace.Debug(1, string.Join("", aMessage));
            }

            DateTime aStartAcquiringTime = DateTime.Now;

            Monitor.Enter(myObj);

#if !NET45
            lock (myAcquiredLocks)
            {
                myAcquiredLocks[myObj] = Thread.CurrentThread;
            }
#endif

            DateTime aStopAcquiringTime = DateTime.Now;
            TimeSpan anElapsedTime = aStartAcquiringTime - aStartAcquiringTime;

            if (EneterTrace.DetailLevel == EneterTrace.EDetailLevel.Debug)
            {
                lock (myWaitings)
                {
                    int aNumberOfWaitings;
                    myWaitings.TryGetValue(obj, out aNumberOfWaitings);
                    --aNumberOfWaitings;
                    if (aNumberOfWaitings <= 0)
                    {
                        myWaitings.Remove(obj);
                    }
                    else
                    {
                        myWaitings[obj] = aNumberOfWaitings;
                    }
                }

                string[] aMessage = { "LOCKED ", anElapsedTime.ToString() };
                EneterTrace.Debug(1, string.Join("", aMessage));
            }

            if (anElapsedTime >= TimeSpan.FromMilliseconds(1000))
            {
                string[] aMessage = { "Locked after [ms]: ", anElapsedTime.TotalMilliseconds.ToString() };
                EneterTrace.Warning(1, string.Join("", aMessage));
            }

            myLockTime = aStopAcquiringTime;
        }

        public static ThreadLock Lock(object obj)
        {
            return new ThreadLock(obj);
        }

        public void Dispose()
        {
            if (myObj == null)
            {
                return;
            }

#if !NET45
            lock (myAcquiredLocks)
            {
                myAcquiredLocks.Remove(myObj);
            }
#endif

            Monitor.Exit(myObj);

            if (myObj != null)
            {
                TimeSpan anElapsedTime = DateTime.Now - myLockTime;

                if (EneterTrace.DetailLevel == EneterTrace.EDetailLevel.Debug)
                {
                    string[] aMessage = { "UNLOCKED ", anElapsedTime.ToString() };
                    EneterTrace.Debug(1, string.Join("", aMessage));
                }
                if (anElapsedTime >= TimeSpan.FromMilliseconds(1000))
                {
                    string[] aMessage = { "Unlocked after [ms]: ", anElapsedTime.TotalMilliseconds.ToString() };
                    EneterTrace.Warning(1, string.Join("", aMessage));
                }


            }
        }


        private bool IsEntered(object obj)
        {
#if NET45
            return Monitor.IsEntered(obj);
#else
            Thread aThread;

            lock (myAcquiredLocks)
            {
                myAcquiredLocks.TryGetValue(obj, out aThread);
            }

            if (aThread != null)
            {
                if (Thread.CurrentThread == aThread)
                {
                    return true;
                }
            }

            return false;
#endif
        }


        private DateTime myLockTime;
        private object myObj;
        private static Dictionary<object, int> myWaitings = new Dictionary<object, int>();

#if !NET45
        private static Dictionary<object, Thread> myAcquiredLocks = new Dictionary<object, Thread>();
#endif
    }
}
