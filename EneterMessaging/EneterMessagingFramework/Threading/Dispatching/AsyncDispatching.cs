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
    /// Provides dispatcher that invokes methods asynchronously using ThreadPool.
    /// </summary>
    public class AsyncDispatching : IDispatcherProvider
    {
        private class AsyncDispatcher : IDispatcher
        {
            public void Invoke(Action workItem)
            {
                ThreadPool.QueueUserWorkItem(x => workItem());
            }
        }

        /// <summary>
        /// Returns dispatcher that invokes methods using TrheadPool.
        /// </summary>
        /// <returns></returns>
        public IDispatcher GetDispatcher()
        {
            return myDispatcher;
        }

        private IDispatcher myDispatcher = new AsyncDispatcher();
    }
}
