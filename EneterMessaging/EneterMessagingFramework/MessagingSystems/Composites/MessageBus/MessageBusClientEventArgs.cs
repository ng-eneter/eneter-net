/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/

using System;

namespace Eneter.Messaging.MessagingSystems.Composites.MessageBus
{
    /// <summary>
    /// Event arguments used by the message bus when a client is connected/disconnected.
    /// </summary>
    public sealed class MessageBusClientEventArgs : EventArgs
    {
        /// <summary>
        /// Constructs the event arguments.
        /// </summary>
        /// <param name="serviceAddress">id of service</param>
        /// <param name="serviceResponseReceiverId">response receiver id of the service.</param>
        /// <param name="clientResponseReceiverId">response receiver id of the client.</param>
        public MessageBusClientEventArgs(string serviceAddress, string serviceResponseReceiverId, string clientResponseReceiverId)
        {
            ServiceAddress = serviceAddress;
            ServiceResponseReceiverId = serviceResponseReceiverId;
            ClientResponseReceiverId = clientResponseReceiverId;
        }

        /// <summary>
        /// Returns service id.
        /// </summary>
        public string ServiceAddress { get; private set; }

        /// <summary>
        /// Rrturns response receiver id of the service.
        /// </summary>
        public string ServiceResponseReceiverId { get; private set; }

        /// <summary>
        /// Returns response receiver id of the client.
        /// </summary>
        public string ClientResponseReceiverId { get; private set; }
    }
}
