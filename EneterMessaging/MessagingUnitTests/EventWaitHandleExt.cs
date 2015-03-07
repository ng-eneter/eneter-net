using System;
using System.Diagnostics;
using System.Threading;

namespace Eneter.MessagingUnitTests
{
    internal static class EventWaitHandleExt
    {
        public static void WaitIfNotDebugging(this EventWaitHandle waitHandle, int milliseconds)
        {
            if (!Debugger.IsAttached)
            {
                if (!waitHandle.WaitOne(milliseconds))
                {
                    throw new TimeoutException("Timeout " + milliseconds + " ms.");
                }
            }
            else
            {
                waitHandle.WaitOne();
            }
        }
    }
}
