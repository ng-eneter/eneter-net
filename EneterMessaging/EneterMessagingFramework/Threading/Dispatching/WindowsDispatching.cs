/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/


#if !MONO && !NET35 && !SILVERLIGHT && !COMPACT_FRAMEWORK && !XAMARIN

using System;
using System.Threading;
using System.Windows.Threading;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.Threading.Dispatching
{
    /// <summary>
    /// Invokes via .NET built in queue System.Windows.Threading.Dispatcher.
    /// </summary>
    /// <remarks>
    /// E.g. in case of WPF it allows to receive message in the UI thread.
    /// </remarks>
    public class WindowsDispatching : IThreadDispatcherProvider
    {
        private class WindowsDispatcher : IThreadDispatcher
        {
            public WindowsDispatcher(Dispatcher workingThreadDispatcher)
            {
                myWorkingThreadDispatcher = workingThreadDispatcher;

                myDispatcherTraceMessage = "TO ~" + workingThreadDispatcher.Thread.ManagedThreadId;
            }

            public void Invoke(Action workItem)
            {
                using (EneterTrace.Entering())
                {
                    EneterTrace.Debug(myDispatcherTraceMessage);
                    myWorkingThreadDispatcher.BeginInvoke(workItem);
                }
            }

            private Dispatcher myWorkingThreadDispatcher;

            private string myDispatcherTraceMessage;
        }

        /// <summary>
        /// Constructs dispatching which uses windows Dispatcher for invoking incoming methods.
        /// </summary>
        /// <param name="windowsDispatcher">windows thread dispatcher.
        /// E.g. in case of using WPF you can provide the dispatcher associated with the UI thread
        /// and then GetDispatcher() method will return the dispatcher that will route all methods
        /// to the UI thread.<br />
        /// You also can crate your own windows dispatcher that is not associated with UI.
        /// You can use WindowsDispatching.StartNewWindowsDispatcher() method.
        /// </param>
        public WindowsDispatching(Dispatcher windowsDispatcher)
        {
            using (EneterTrace.Entering())
            {
                myDispatcher = new WindowsDispatcher(windowsDispatcher);
            }
        }

        /// <summary>
        /// Returns dispatcher which invokes incoming methods using the windows dispatcher.
        /// </summary>
        /// <returns></returns>
        public IThreadDispatcher GetDispatcher()
        {
            return myDispatcher;
        }

        /// <summary>
        /// Helper method starting a new thread and creating and starting the windows dispatcher for it.
        /// </summary>
        /// <remarks>
        /// This method starts the new thread and then creates the windows dispatcher for it.
        /// The thread runs in the loop and processes queued methods.<br/>
        /// Do not forget to call StopWindowsDispatcher() to release the looping thread.
        /// Otherwise the thread will leak and can cause the application will hang when closed.
        /// </remarks>
        /// <returns></returns>
        public static Dispatcher StartNewWindowsDispatcher()
        {
            using (EneterTrace.Entering())
            {
                Dispatcher aDispatcher = null;

                // Create the working thread and activate the dispatcher for it.
                ManualResetEvent aDispatcherReadySignal = new ManualResetEvent(false);
                Thread aWorkingThread = new Thread(() =>
                    {
                        // Create the dispatcher for the current thread.
                        aDispatcher = Dispatcher.CurrentDispatcher;

                        // Signal that the dispatcher is created.
                        aDispatcherReadySignal.Set();

                        // Runn the loop processing dispatched requests.
                        Dispatcher.Run();
                    });
                aWorkingThread.IsBackground = true;
                aWorkingThread.Start();

                // Wait until the dispatcher is created.
                if (!aDispatcherReadySignal.WaitOne(5000))
                {
                    throw new TimeoutException("Working thread failed to start the dispatcher within 5000 ms.");
                }

                return aDispatcher;
            }
        }

        /// <summary>
        /// Releases the thread waiting in the windows dispatcher queue for delegates that shall be invoked.
        /// </summary>
        /// <param name="dispatcher">dispatcher that shall be stopped</param>
        public static void StopWindowsDispatcher(Dispatcher dispatcher)
        {
            using (EneterTrace.Entering())
            {
                dispatcher.InvokeShutdown();
            }
        }

        private IThreadDispatcher myDispatcher;
    }
}

#endif