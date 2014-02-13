/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2014
*/

using System;
using System.Collections.Generic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.Composites.MessageBus
{
    public interface IMessageBus
    {
		event EventHandler<MessageBusServiceEventArgs> ServiceConnected;
		event EventHandler<MessageBusServiceEventArgs> ServiceDisconnected;

        void AttachDuplexInputChannels(IDuplexInputChannel serviceInputChannel, IDuplexInputChannel clientInputChannel);
        void DetachDuplexInputChannels();

		IEnumerable<string> ConnectedServices { get; }

		void DisconnectService(string serviceAddress);
    }
}
