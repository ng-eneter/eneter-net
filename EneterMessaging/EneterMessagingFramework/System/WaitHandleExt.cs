/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/

#if COMPACT_FRAMEWORK

namespace System.Threading
{
    /// <summary>
    /// Provides WaitOne(int miliseconds) method for the compact framework.
    /// </summary>
    internal static class WaitHandleExt
    {
        public static bool WaitOne(this WaitHandle waitHandler, int millisecondsTimeout)
        {
            return waitHandler.WaitOne(millisecondsTimeout, false);
        }
    }
}

#endif