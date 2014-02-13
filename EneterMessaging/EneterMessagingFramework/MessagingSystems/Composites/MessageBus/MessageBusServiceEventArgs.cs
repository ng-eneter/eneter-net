/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2014
*/

using System;

namespace Eneter.Messaging.MessagingSystems.Composites.MessageBus
{
	public sealed class MessageBusServiceEventArgs : EventArgs
	{
		public MessageBusServiceEventArgs(string serviceAddress)
		{
			ServiceAddress = serviceAddress;
		}

		public string ServiceAddress { get; private set; }
	}
}
