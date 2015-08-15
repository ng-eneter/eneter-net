/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/

using System;
using System.Threading;

namespace Eneter.Messaging.Diagnostic
{
    internal class ThreadLock : IDisposable
    {
        private IDisposable myEneterTimingTrace;
        private object myObj;

        private ThreadLock(object obj)
        {
            myObj = obj;

            using (EneterTrace.TimeTracking("LOCKING"))
            {
                Monitor.Enter(myObj);
            }

            myEneterTimingTrace = EneterTrace.TimeTracking("LOCKED");
        }

        public static ThreadLock Lock(object obj)
        {
            return new ThreadLock(obj);
        }

        public void Dispose()
        {
            Monitor.Exit(myObj);
            if (myEneterTimingTrace != null)
            {
                myEneterTimingTrace.Dispose();
            }
        }
    }
}
