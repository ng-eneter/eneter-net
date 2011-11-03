using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.MessagingUnitTests.Nodes.ChannelWrapper
{
    internal class Mock_DuplexOutputChannelAttachable : IAttachableDuplexOutputChannel
    {
        public event EventHandler<DuplexChannelMessageEventArgs> ResponseMessageReceived;

        public void Attach(IDuplexOutputChannel duplexOutputChannel)
        {
            lock (this)
            {
                if (AttachedDuplexOutputChannel != null)
                {
                    throw new InvalidOperationException("The duplex output channel is already attached.");
                }

                AttachedDuplexOutputChannel = duplexOutputChannel;
                AttachedDuplexOutputChannel.ResponseMessageReceived += OnResponseMessageReceived;
            }
        }

        public void AttachDuplexOutputChannel(IDuplexOutputChannel duplexOutputChannel)
        {
            lock (this)
            {
                Attach(duplexOutputChannel);

                AttachedDuplexOutputChannel.OpenConnection();
            }
        }

        public void DetachDuplexOutputChannel()
        {
            lock (this)
            {
                if (AttachedDuplexOutputChannel != null)
                {
                    AttachedDuplexOutputChannel.CloseConnection();
                    AttachedDuplexOutputChannel.ResponseMessageReceived -= OnResponseMessageReceived;
                    AttachedDuplexOutputChannel = null;
                }
            }
        }

        public bool IsDuplexOutputChannelAttached
        {
            get
            {
                lock (this)
                {
                    return AttachedDuplexOutputChannel != null;
                }
            }
        }

        public void SendMessage(string message)
        {
            lock (this)
            {
                AttachedDuplexOutputChannel.SendMessage(message);
            }
        }

        protected void OnResponseMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            if (ResponseMessageReceived != null)
            {
                ResponseMessageReceived(this, e);
            }
        }

        public IDuplexOutputChannel AttachedDuplexOutputChannel { get; private set; }
    }
}
