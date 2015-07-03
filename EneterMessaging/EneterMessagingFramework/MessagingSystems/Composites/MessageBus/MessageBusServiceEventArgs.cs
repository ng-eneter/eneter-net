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
        /// <param name="responseReceiverId">response receiver id of the service.</param>
		public MessageBusServiceEventArgs(string serviceAddress, string responseReceiverId)
		{
			ServiceAddress = serviceAddress;
            ResponseReceiverId = responseReceiverId;
		}

        /// <summary>
        /// Returns service id.
        /// </summary>
		public string ServiceAddress { get; private set; }

        /// <summary>
        /// Returns response receiver id of the service.
        /// </summary>
        public string ResponseReceiverId { get; private set; }
	}
}
