/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/

using System;
using System.Diagnostics;
using System.Threading;

namespace Eneter.Messaging.Diagnostic
{
    internal class ThreadLock : IDisposable
    {
        private Stopwatch myStopWatch;
        private object myObj;

        private ThreadLock(object obj)
        {
            myObj = obj;

            EneterTrace.Debug("LOCKING");
            Stopwatch aStopWatch = Stopwatch.StartNew();
            Monitor.Enter(myObj);
            aStopWatch.Stop();
            EneterTrace.Debug(string.Join(" ", "LOCKED", aStopWatch.Elapsed));

            myStopWatch = Stopwatch.StartNew();
        }

        public static ThreadLock Lock(object obj)
        {
            return new ThreadLock(obj);
        }

        public void Dispose()
        {
            Monitor.Exit(myObj);
            myStopWatch.Stop();
            EneterTrace.Debug(string.Join(" ", "UNLOCKED", myStopWatch.Elapsed));
        }
    }
}
