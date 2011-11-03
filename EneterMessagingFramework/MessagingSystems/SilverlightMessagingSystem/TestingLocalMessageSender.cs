/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if SILVERLIGHT && !WINDOWS_PHONE

using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.SilverlightMessagingSystem
{
    /// <summary>
    /// Testing mock for easy replacing of real silverlight LocalMessageSender during unit testing.
    /// E.g. if you want to perform tests with NUnit the silverlight main thread is not available.
    /// Therefore you can provide here some other messaging e.g. synchronous messaging or thread messaging.
    /// </summary>
    public class TestingLocalMessageSender : ILocalMessageSender
    {
        /// <summary>
        /// Constructs the testing mock from input parameters.
        /// </summary>
        /// <param name="receiverName">Receiver identifier in silverlight.</param>
        /// <param name="messagingSystemFactory">Messaging that will replace the silverlight messaging</param>
        public TestingLocalMessageSender(string receiverName, IMessagingSystemFactory messagingSystemFactory)
        {
            using (EneterTrace.Entering())
            {
                myOutputChannel = messagingSystemFactory.CreateOutputChannel(receiverName);
            }
        }

        /// <summary>
        /// Sends the message as if from original silverlight LocalMessageSender.
        /// </summary>
        /// <param name="message">message</param>
        public void SendAsync(string message)
        {
            using (EneterTrace.Entering())
            {
                myOutputChannel.SendMessage(message);
            }
        }

        /// <summary>
        /// Receiver identifier in silverlight.
        /// </summary>
        public string ReceiverName
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    return myOutputChannel.ChannelId;
                }
            }
        }

        private IOutputChannel myOutputChannel;
    }
}

#endif
