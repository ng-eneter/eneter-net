/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if SILVERLIGHT && !WINDOWS_PHONE

using System;
using System.Windows.Messaging;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.SilverlightMessagingSystem
{
    internal class SilverlightLocalMessageReceiver : ILocalMessageReceiver
    {
        public event EventHandler<ChannelMessageEventArgs> MessageReceived;

        public SilverlightLocalMessageReceiver(string receiverName)
        {
            using (EneterTrace.Entering())
            {
                MessageReceiver = new LocalMessageReceiver(receiverName);
                MessageReceiver.MessageReceived += OnMessageReceived;
            }
        }

        public string ReceiverName
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    return MessageReceiver.ReceiverName;
                }
            }
        }

        public void Listen()
        {
            using (EneterTrace.Entering())
            {
                MessageReceiver.Listen();
            }
        }


        public void Dispose()
        {
            MessageReceiver.MessageReceived -= OnMessageReceived;
            MessageReceiver.Dispose();
            MessageReceiver = null;
        }

        private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (MessageReceived != null)
                {
                    MessageReceived(this, new ChannelMessageEventArgs(ReceiverName, e.Message));
                }
            }
        }

        private LocalMessageReceiver MessageReceiver { get; set; }
    }
}

#endif
