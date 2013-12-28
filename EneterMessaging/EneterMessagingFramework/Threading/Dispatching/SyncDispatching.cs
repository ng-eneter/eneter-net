/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.Threading.Dispatching
{
    /// <summary>
    /// Provides dispatcher that queues incoming methods and calls them synchronously one by one.
    /// </summary>
    public class SyncDispatching : IThreadDispatcherProvider
    {
        /// <summary>
        /// Constructs dispatching where each GetDispatcher() returns a new instance of the dispatcher.
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
