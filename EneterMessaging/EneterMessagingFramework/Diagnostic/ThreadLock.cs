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
            myObj = obj;

            if (Monitor.IsEntered(obj))
            {
                Monitor.Enter(obj);
                return;
            }
            
            if (EneterTrace.DetailLevel == EneterTrace.EDetailLevel.Debug)
            {
                int aNumberOfWaitings;
                lock (myWaitings)
                {
                    myWaitings.TryGetValue(obj, out aNumberOfWaitings);
                    ++aNumberOfWaitings;
                    myWaitings[obj] = aNumberOfWaitings;
                }

                EneterTrace.Debug(1, string.Join("", "LOCKING ", aNumberOfWaitings));
            }

            DateTime aStartAcquiringTime = DateTime.Now;
            Monitor.Enter(myObj);
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

                EneterTrace.Debug(1, string.Join("", "LOCKED ", anElapsedTime));
            }

            if (anElapsedTime >= TimeSpan.FromMilliseconds(1000))
            {
                EneterTrace.Warning(1, string.Join("", "Locked after [ms]: ", anElapsedTime.TotalMilliseconds));
            }

            myLockTime = aStopAcquiringTime;
        }

        public static ThreadLock Lock(object obj)
        {
            return new ThreadLock(obj);
        }

        public void Dispose()
        {
            Monitor.Exit(myObj);

            if (!Monitor.IsEntered(myObj))
            {
                TimeSpan anElapsedTime = DateTime.Now - myLockTime;

                if (EneterTrace.DetailLevel == EneterTrace.EDetailLevel.Debug)
                {
                    EneterTrace.Debug(1, string.Join(" ", "UNLOCKED", anElapsedTime));
                }
                if (anElapsedTime >= TimeSpan.FromMilliseconds(1000))
                {
                    EneterTrace.Warning(1, string.Join("", "Unlocked after [ms]: ", anElapsedTime.TotalMilliseconds));
                }
            }
        }


        private DateTime myLockTime;
        private object myObj;
        private static Dictionary<object, int> myWaitings = new Dictionary<object, int>();
    }
}
