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
    /// Dispatcher that invokes the callback methods as is - without marshaling them into a particular thread. 
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
        /// Returns dispatcher that invokes the callback method immediately without marshaling into a thread.
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
