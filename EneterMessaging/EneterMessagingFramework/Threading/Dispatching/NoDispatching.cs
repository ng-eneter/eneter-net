/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/


using System;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.Threading.Dispatching
{
    /// <summary>
    /// Provides dispatcher that just invokes the method directly from the calling thread.
    /// </summary>
    public class NoDispatching : IThreadDispatcherProvider
    {
        private class DefaultDispatcher : IThreadDispatcher
        {
            public void Invoke(Action workItem)
            {
                // Just invoke the delegate.
                workItem();
            }
        }

        /// <summary>
        /// Returns dispatcher invoking the method directly from the calling thread.
        /// </summary>
        /// <returns></returns>
        public IThreadDispatcher GetDispatcher()
        {
            using (EneterTrace.Entering())
            {
                return myDispatcher;
            }
        }

        private static DefaultDispatcher myDispatcher = new DefaultDispatcher();
    }
}
