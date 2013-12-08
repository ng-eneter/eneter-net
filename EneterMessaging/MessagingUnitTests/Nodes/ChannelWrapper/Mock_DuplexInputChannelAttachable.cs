using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.MessagingUnitTests.Nodes.ChannelWrapper
{
    internal class Mock_DuplexInputChannelAttachable : IAttachableDuplexInputChannel
    {
        public event EventHandler<DuplexChannelMessageEventArgs> MessageReceived;

        public void Attach(IDuplexInputChannel duplexInputChannel)
        {
            lock (this)
            {
                if (DuplexInputChannel != null)
                {
                    throw new InvalidOperationException("The duplex input channel is already attached.");
                }

                DuplexInputChannel = duplexInputChannel;
                DuplexInputChannel.MessageReceived += OnMessageReceived;
            }
        }

        public void AttachDuplexInputChannel(IDuplexInputChannel duplexInputChannel)
        {
            lock (this)
            {
                Attach(duplexInputChannel);
                DuplexInputChannel.StartListening();
            }
        }

        public void DetachDuplexInputChannel()
        {
            lock (this)
            {
                if (DuplexInputChannel != null)
                {
                    DuplexInputChannel.StopListening();
                    DuplexInputChannel.MessageReceived -= OnMessageReceived;
                    DuplexInputChannel = null;
                }
            }
        }

        public bool IsDuplexInputChannelAttached
        {
            get
            {
                lock (this)
                {
                    return DuplexInputChannel != null;
                }
            }
        }

        public IDuplexInputChannel AttachedDuplexInputChannel
        {
            get { return DuplexInputChannel; }
        }

        public void SendResponseMessage(string receiverId, string message)
        {
            lock (this)
            {
                DuplexInputChannel.SendResponseMessage(receiverId, message);
            }
        }

        protected void OnMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            if (MessageReceived != null)
            {
                MessageReceived(this, e);
            }
        }

        protected IDuplexInputChannel DuplexInputChannel { get; set; }
    }
}
