/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/


#if SILVERLIGHT && !WINDOWS_PHONE

using System;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.SilverlightMessagingSystem
{
    internal class TestingLocalMessageReceiver : ILocalMessageReceiver
    {
        public event EventHandler<ChannelMessageEventArgs> MessageReceived;

        public TestingLocalMessageReceiver(string receiverName, IMessagingSystemFactory messagingSystemFactory)
        {
            using (EneterTrace.Entering())
            {
                myInputChannel = messagingSystemFactory.CreateInputChannel(receiverName);
                myInputChannel.MessageReceived += OnMessageReceived;
            }
        }

        public string ReceiverName { get { return myInputChannel.ChannelId; } }

        public void Listen()
        {
            using (EneterTrace.Entering())
            {
                myInputChannel.StartListening();
            }
        }


        public void Dispose()
        {
            myInputChannel.StopListening();
        }

        private void OnMessageReceived(object sender, ChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (MessageReceived != null)
                {
                    MessageReceived(this, e);
                }
            }
        }

        private IInputChannel myInputChannel;
    }
}

#endif
