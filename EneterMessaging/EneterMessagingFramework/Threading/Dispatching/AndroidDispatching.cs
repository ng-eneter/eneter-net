

#if MONOANDROID

using Android.OS;
using Eneter.Messaging.Diagnostic;
using System;

namespace Eneter.Messaging.Threading.Dispatching
{
    /// <summary>
    /// Invokes one by one using Android Handler mechanism (e.g. to invoke in the UI thread).
    /// </summary>
    /// <remarks>
    /// This dispatcher is available only for the Android android platform.
    /// </remarks>
    public class AndroidDispatching : IThreadDispatcherProvider
    {
        private class AndroidDispatcher : IThreadDispatcher
        {
            public AndroidDispatcher(Handler threadDispatcher)
            {
                myDispatcher = threadDispatcher;
            }

            public void Invoke(Action workItem)
            {
                myDispatcher.Post(workItem);
            }

            private Handler myDispatcher;
        }

        /// <summary>
        /// Constructs dispatcher.
        /// </summary>
        /// <param name="threadDispatcher">Android handler used for dispatching.</param>
        public AndroidDispatching(Handler threadDispatcher)
        {
            using (EneterTrace.Entering())
            {
                myThreadDispatcher = new AndroidDispatcher(threadDispatcher);
            }
        }

        /// <summary>
        /// Returns the thread dispatcher which uses the Android Handler class.
        /// </summary>
        /// <returns></returns>
        public IThreadDispatcher GetDispatcher()
        {
            return myThreadDispatcher;
        }

        private IThreadDispatcher myThreadDispatcher;
    }
}

#endif