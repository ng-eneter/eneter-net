/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

using System;
using System.Threading;

namespace Eneter.Messaging.Threading.Dispatching
{
    /// <summary>
    /// Dispatcher that invokes callback methods asynchronously in a thread from the thread-pool.
    /// </summary>
    public class AsyncDispatching : IThreadDispatcherProvider
    {
        private class AsyncDispatcher : IThreadDispatcher
        {
            public void Invoke(Action workItem)
            {
                ThreadPool.QueueUserWorkItem(x => workItem());
            }
        }

        /// <summary>
        /// Returns dispatcher that invokes the callback method asynchronously in a thread from the thread-pool.
        /// </summary>
        /// <returns></returns>
        public IThreadDispatcher GetDispatcher()
        {
            return myDispatcher;
        }

        private static IThreadDispatcher myDispatcher = new AsyncDispatcher();
    }
}
