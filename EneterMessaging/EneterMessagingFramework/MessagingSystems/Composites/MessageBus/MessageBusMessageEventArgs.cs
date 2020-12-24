

using System;

namespace Eneter.Messaging.MessagingSystems.Composites.MessageBus
{
    /// <summary>
    /// Event arguments used by message when a message was transferred to a service or to a client.
    /// </summary>
    public sealed class MessageBusMessageEventArgs : EventArgs
    {
        /// <summary>
        /// Constructs the event arguments.
        /// </summary>
        /// <param name="serviceAddress">id of service.</param>
        /// <param name="serviceResponseReceiverId">response receiver id of the service.</param>
        /// <param name="clientResponseReceiverId">response receiver id of the client.</param>
        /// <param name="message">message which is sent from client to service or from service to client.</param>
        public MessageBusMessageEventArgs(string serviceAddress, string serviceResponseReceiverId, string clientResponseReceiverId, object message)
        {
            ServiceAddress = serviceAddress;
            ServiceResponseReceiverId = serviceResponseReceiverId;
            ClientResponseReceiverId = clientResponseReceiverId;
            Message = message;
        }

        /// <summary>
        /// Returns service id.
        /// </summary>
        public string ServiceAddress { get; private set; }

        /// <summary>
        /// Returns response receiver id of the service.
        /// </summary>
        public string ServiceResponseReceiverId { get; private set; }

        /// <summary>
        /// Returns response receiver id of the client.
        /// </summary>
        public string ClientResponseReceiverId { get; private set; }

        /// <summary>
        /// Returns message which is between client and service.
        /// </summary>
        public object Message { get; private set; }
    }
}
