/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if SILVERLIGHT && !WINDOWS_PHONE

using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;

namespace Eneter.Messaging.MessagingSystems.SilverlightMessagingSystem
{
    /// <summary>
    /// The class implements the factory used for the testing purposes when you need to emulate
    /// silverlight messaging. E.g. when you test with NUnit the main silverlight thread is not available.
    /// Therefore the messaging will not work and you may want to mock it.
    /// This factory uses SynchronousMessagingSystemFactory to emulate silverlight messaging.
    /// </summary>
    public class TestingLocalSenderReceiverFactory : ILocalSenderReceiverFactory
    {
        /// <summary>
        /// Constructs the factory.
        /// </summary>
        public TestingLocalSenderReceiverFactory()
        {
            using (EneterTrace.Entering())
            {
                myMessagingSystemFactory = new SynchronousMessagingSystemFactory();
            }
        }

        /// <summary>
        /// Creates mock of the LocalMessageSender based on the SynchronousMessagingSystemFactory.
        /// </summary>
        /// <param name="receiverName">silverlight receiver identifier</param>
        public ILocalMessageSender CreateLocalMessageSender(string receiverName)
        {
            using (EneterTrace.Entering())
            {
                return new TestingLocalMessageSender(receiverName, myMessagingSystemFactory);
            }
        }

        /// <summary>
        /// Creates mock of the LocalMessageReceiver based on the SynchronousMessagingSystemFactory.
        /// </summary>
        /// <param name="receiverName">silverlight receiver identifier</param>
        public ILocalMessageReceiver CreateLocalMessageReceiver(string receiverName)
        {
            using (EneterTrace.Entering())
            {
                return new TestingLocalMessageReceiver(receiverName, myMessagingSystemFactory);
            }
        }

        private IMessagingSystemFactory myMessagingSystemFactory;
    }
}

#endif
