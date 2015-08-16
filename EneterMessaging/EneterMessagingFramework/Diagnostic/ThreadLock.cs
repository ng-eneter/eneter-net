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
        private Stopwatch myStopWatch;
        private object myObj;
        private static Dictionary<object, int> myWaitings = new Dictionary<object, int>();

        private ThreadLock(object obj)
        {
            if (!Monitor.IsEntered(obj))
            {
                myObj = obj;
                int aNumberOfWaitings;

                if (EneterTrace.DetailLevel == EneterTrace.EDetailLevel.Debug)
                {
                    lock (myWaitings)
                    {
                        myWaitings.TryGetValue(obj, out aNumberOfWaitings);
                        ++aNumberOfWaitings;
                        myWaitings[obj] = aNumberOfWaitings;
                    }

                    EneterTrace.Debug(1, string.Join("", "LOCKING ", aNumberOfWaitings));
                }

                Stopwatch aStopWatch = Stopwatch.StartNew();
                Monitor.Enter(myObj);
                aStopWatch.Stop();

                if (EneterTrace.DetailLevel == EneterTrace.EDetailLevel.Debug)
                {
                    lock (myWaitings)
                    {
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

                    EneterTrace.Debug(1, string.Join("", "LOCKED ", aStopWatch.Elapsed));
                }

                if (aStopWatch.Elapsed >= TimeSpan.FromMilliseconds(10))
                {
                    EneterTrace.Warning(1, string.Join("", "Locked after [ms]: ", aStopWatch.ElapsedMilliseconds));
                }

                myStopWatch = Stopwatch.StartNew();
            }
        }

        public static ThreadLock Lock(object obj)
        {
            return new ThreadLock(obj);
        }

        public void Dispose()
        {
            if (myObj != null)
            {
                Monitor.Exit(myObj);
                myStopWatch.Stop();
                EneterTrace.Debug(1, string.Join(" ", "UNLOCKED", myStopWatch.Elapsed));
                if (myStopWatch.Elapsed >= TimeSpan.FromMilliseconds(10))
                {
                    EneterTrace.Warning(1, string.Join("", "Unlocked after [ms]: ", myStopWatch.ElapsedMilliseconds));
                }
            }
        }
    }
}
