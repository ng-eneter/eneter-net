/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2014
*/

using System;

namespace Eneter.Messaging.MessagingSystems.Composites.MessageBus
{
    /// <summary>
    /// Event arguments used by the message bus when a service is connected / disconnected.
    /// </summary>
	public sealed class MessageBusServiceEventArgs : EventArgs
	{
        /// <summary>
        /// Constructs the event arguments.
        /// </summary>
        /// <param name="serviceAddress">service id.</param>
		public MessageBusServiceEventArgs(string serviceAddress)
		{
			ServiceAddress = serviceAddress;
		}

        /// <summary>
        /// Returns service id.
        /// </summary>
		public string ServiceAddress { get; private set; }
	}
}
