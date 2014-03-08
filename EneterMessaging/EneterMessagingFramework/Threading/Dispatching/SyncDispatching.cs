/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

using System;
using Eneter.Messaging.DataProcessing.MessageQueueing;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.Threading.Dispatching
{
    /// <summary>
    /// Dispatcher that queues callback methods and invokes them one by one from one thread.
    /// </summary>
    public class SyncDispatching : IThreadDispatcherProvider
    {
        private class SyncDispatcher : IThreadDispatcher
        {
            public void Invoke(Action workItem)
            {
                myWorkingThread.Execute(workItem);
            }

            private SingleThreadExecutor myWorkingThread = new SingleThreadExecutor();
        }

        /// <summary>
        /// Constructs dispatching where each GetDispatcher() will return new instance of the dispatcher.
        /// </summary>
        public SyncDispatching()
            : this(false)
        {
        }

        /// <summary>
        /// Constructor which allows to specify if GetDispatcher() returns always the same dispatcher or GetDispatcher()
        /// returns always the new instance the dispatcher.
        /// </summary>
        /// <param name="isDispatcherShared">
        /// true - GetDispatcher() will return always the same instance of the dispatcher. It means all dispatchers returned from
        /// GetDispatcher() will sync incoming methods using the same queue. <br/>
        /// false - GetDispatcher() will return always the new instance of the dispatcher. It means each dispatcher returned from
        /// GetDispatcher() will use its own synchronization queue.
        /// </param>
        public SyncDispatching(bool isDispatcherShared)
        {
            if (isDispatcherShared)
            {
                mySharedDispatcher = new SyncDispatcher();
            }
        }

        /// <summary>
        /// Returns dispatcher that sync all incoming methods into one thread.
        /// </summary>
        /// <remarks>
        /// If SyncDispatching was created with isDispatcherShared true then it always returns the same instance
        /// of the thread dispatcher. Otherwise it always creates the new one. 
        /// </remarks>
        /// <returns></returns>
        public IThreadDispatcher GetDispatcher()
        {
            using (EneterTrace.Entering())
            {
                return (mySharedDispatcher != null) ? mySharedDispatcher : new SyncDispatcher();
            }
        }

        private IThreadDispatcher mySharedDispatcher;
    }
}
