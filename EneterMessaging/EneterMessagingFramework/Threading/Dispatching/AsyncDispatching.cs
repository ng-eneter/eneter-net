

using System;
using System.Threading;

namespace Eneter.Messaging.Threading.Dispatching
{
    /// <summary>
    /// Invokes asynchronously by routing to a thread from the thread-pool (each method can be executed in a separate thread).
    /// </summary>
    public class AsyncDispatching : IThreadDispatcherProvider
    {
        private class AsyncDispatcher : IThreadDispatcher
        {
            public void Invoke(Action workItem)
            {
                EneterThreadPool.QueueUserWorkItem(workItem);
            }
        }

        /// <summary>
        /// Returns dispatcher which invokes asynchronously in a thread from the thread-pool.
        /// </summary>
        /// <returns></returns>
        public IThreadDispatcher GetDispatcher()
        {
            return myDispatcher;
        }

        private static IThreadDispatcher myDispatcher = new AsyncDispatcher();
    }
}
