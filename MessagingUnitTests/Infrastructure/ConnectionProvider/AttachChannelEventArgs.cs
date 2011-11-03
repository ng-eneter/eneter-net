using System;

namespace Eneter.MessagingUnitTests.Infrastructure.ConnectionProvider
{
    internal class AttachChannelEventArgs : EventArgs
    {
        public AttachChannelEventArgs(string channelId)
        {
            ChannelId = channelId;
        }

        public string ChannelId { get; private set; }
    }
}
