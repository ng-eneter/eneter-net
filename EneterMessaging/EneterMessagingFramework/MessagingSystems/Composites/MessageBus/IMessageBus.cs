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
		event EventHandler<MessageBusServiceEventArgs> ServiceRegistered;
		event EventHandler<MessageBusServiceEventArgs> ServiceUnregistered;

        void AttachDuplexInputChannels(IDuplexInputChannel serviceInputChannel, IDuplexInputChannel clientInputChannel);
        void DetachDuplexInputChannels();

		IEnumerable<string> ConnectedServices { get; }

		void DisconnectService(string serviceAddress);
    }
}
