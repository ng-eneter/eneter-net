

#if !NET35 && NETFRAMEWORK

using System;
using System.Diagnostics;
using System.Threading;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.SharedMemoryMessagingSystem
{
    internal class EventWaitHandleExt
    {
        // Note: if openTimeout <= 0 then infinite loop trying to open the handle.
        public static EventWaitHandle OpenExisting(string name, TimeSpan openTimeout)
        {
            using (EneterTrace.Entering())
            {
                EventWaitHandle anEventWaitHandle = null;

                Stopwatch aStopWatch = new Stopwatch();
                aStopWatch.Start();
                try
                {
                    while (true)
                    {
                        try
                        {
                            anEventWaitHandle = EventWaitHandle.OpenExisting(name);

                            // No exception, so the handle was open.
                            break;
                        }
                        catch (WaitHandleCannotBeOpenedException)
                        {
                            // The handle does not exist so check the timeout.
                            if (openTimeout > TimeSpan.Zero && aStopWatch.Elapsed >= openTimeout)
                            {
                                throw;
                            }
                        }

                        // Wait a moment and try again. The handle can be meanwhile created.
                        Thread.Sleep(100);
                    }
                }
                finally
                {
                    aStopWatch.Stop();
                }

                return anEventWaitHandle;
            }
        }
    }
}

#endif