/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using System;

namespace Eneter.Messaging.Infrastructure.ConnectionProvider
{
    [Obsolete]
    internal class ConnectionProvider : IConnectionProvider
    {
        public ConnectionProvider(IMessagingSystemFactory messagingSystemFactory)
        {
            using (EneterTrace.Entering())
            {
                MessagingSystem = messagingSystemFactory;
            }
        }

        public void Attach(IAttachableDuplexInputChannel inputComponent, string channelId)
        {
            using (EneterTrace.Entering())
            {
                IDuplexInputChannel anInputChannel = MessagingSystem.CreateDuplexInputChannel(channelId);
                inputComponent.AttachDuplexInputChannel(anInputChannel);
            }
        }

        public void Attach(IAttachableMultipleDuplexInputChannels inputComponent, string channelId)
        {
            using (EneterTrace.Entering())
            {
                IDuplexInputChannel anInputChannel = MessagingSystem.CreateDuplexInputChannel(channelId);
                inputComponent.AttachDuplexInputChannel(anInputChannel);
            }
        }

        public void Attach(IAttachableDuplexOutputChannel outputComponent, string channelId)
        {
            using (EneterTrace.Entering())
            {
                IDuplexOutputChannel anOutputChannel = MessagingSystem.CreateDuplexOutputChannel(channelId);
                outputComponent.AttachDuplexOutputChannel(anOutputChannel);
            }
        }

        public void Connect(IAttachableDuplexInputChannel inputComponent, IAttachableDuplexOutputChannel outputComponent, string channelId)
        {
            using (EneterTrace.Entering())
            {
                Attach(inputComponent, channelId);
                Attach(outputComponent, channelId);
            }
        }

        public void Connect(IAttachableMultipleDuplexInputChannels inputComponent, IAttachableDuplexOutputChannel outputComponent, string channelId)
        {
            using (EneterTrace.Entering())
            {
                Attach(inputComponent, channelId);
                Attach(outputComponent, channelId);
            }
        }

        public IMessagingSystemFactory MessagingSystem { get; private set; }
    }
}
