using System;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.Infrastructure.Attachable;
using System.Collections.Generic;

namespace Eneter.MessagingUnitTests.Infrastructure.ConnectionProvider
{
    internal class Mock_Attachable : IAttachableMultipleInputChannels,
                                     IAttachableMultipleOutputChannels,
                                     IAttachableInputChannel,
                                     IAttachableOutputChannel
    {
        public event EventHandler<AttachChannelEventArgs> InputChannelAttached;
        public event EventHandler<AttachChannelEventArgs> OutputChannelAttached;

        public void AttachInputChannel(IInputChannel inputChannel)
        {
            if (InputChannelAttached != null)
            {
                InputChannelAttached(this, new AttachChannelEventArgs(inputChannel.ChannelId));
            }
        }

        public void DetachInputChannel()
        {
            throw new NotImplementedException();
        }

        public void DetachInputChannel(string channelId)
        {
            throw new NotImplementedException();
        }

        public bool IsInputChannelAttached
        {
            get { throw new NotImplementedException(); }
        }


        public void AttachOutputChannel(IOutputChannel outputChannel)
        {
            if (OutputChannelAttached != null)
            {
                OutputChannelAttached(this, new AttachChannelEventArgs(outputChannel.ChannelId));
            }
        }

        public void DetachOutputChannel()
        {
            throw new NotImplementedException();
        }

        public void DetachOutputChannel(string channelId)
        {
            throw new NotImplementedException();
        }

        public bool IsOutputChannelAttached
        {
            get { throw new NotImplementedException(); }
        }


        public IEnumerable<IInputChannel> AttachedInputChannels
        {
            get { throw new NotImplementedException(); }
        }


        public IEnumerable<IOutputChannel> AttachedOutputChannels
        {
            get { throw new NotImplementedException(); }
        }


        public IInputChannel AttachedInputChannel
        {
            get { throw new NotImplementedException(); }
        }


        public IOutputChannel AttachedOutputChannel
        {
            get { throw new NotImplementedException(); }
        }
    }
}
