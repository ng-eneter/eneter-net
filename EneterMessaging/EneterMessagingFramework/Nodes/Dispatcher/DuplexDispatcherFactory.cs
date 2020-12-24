

using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.Nodes.Dispatcher
{
    /// <summary>
    /// Implements factory to create the dispatcher.
    /// </summary>
    public class DuplexDispatcherFactory : IDuplexDispatcherFactory
    {
        /// <summary>
        /// Constructs the duplex dispatcher factory.
        /// </summary>
        /// <param name="duplexOutputChannelsFactory">
        /// the messaging system factory used to create duplex output channels
        /// </param>
        public DuplexDispatcherFactory(IMessagingSystemFactory duplexOutputChannelsFactory)
        {
            using (EneterTrace.Entering())
            {
                myMessagingSystemFactory = duplexOutputChannelsFactory;
            }
        }

        /// <summary>
        /// Creates the duplex dispatcher.
        /// </summary>
        /// <returns>duplex dispatcher</returns>
        public IDuplexDispatcher CreateDuplexDispatcher()
        {
            using (EneterTrace.Entering())
            {
                return new DuplexDispatcher(myMessagingSystemFactory);
            }
        }

        private IMessagingSystemFactory myMessagingSystemFactory;
    }
}
