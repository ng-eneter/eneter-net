

using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.Nodes.Router
{
    /// <summary>
    /// Implements the factory creating duplex router.
    /// </summary>
    public class DuplexRouterFactory : IDuplexRouterFactory
    {
        /// <summary>
        /// Constructs the duplex router factory.
        /// </summary>
        /// <param name="duplexOutputChannelMessaging">
        /// the messaging system factory used to create duplex output channels
        /// </param>
        public DuplexRouterFactory(IMessagingSystemFactory duplexOutputChannelMessaging)
        {
            using (EneterTrace.Entering())
            {
                myDuplexOutputChannelMessaging = duplexOutputChannelMessaging;
            }
        }

        /// <summary>
        /// Creates the duplex router.
        /// </summary>
        /// <returns>duplex router</returns>
        public IDuplexRouter CreateDuplexRouter()
        {
            using (EneterTrace.Entering())
            {
                return new DuplexRouter(myDuplexOutputChannelMessaging);
            }
        }


        private IMessagingSystemFactory myDuplexOutputChannelMessaging;
    }
}
