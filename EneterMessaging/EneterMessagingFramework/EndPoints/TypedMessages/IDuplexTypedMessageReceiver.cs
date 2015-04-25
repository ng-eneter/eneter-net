/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    /// <summary>
    /// Receiver of one specified message type.
    /// </summary>
    /// <remarks>
    /// This is a service component which can receive request messages and send back response messages.
    /// DuplexTypedMessageReceiver can receive messages only from DuplexTypedMessageSender or from SyncDuplexTypedMessageSender.
    /// </remarks>
    /// <typeparam name="TResponse">Type of response message which can be sent by the receiver.</typeparam>
    /// <typeparam name="TRequest">Type of request message which can be received by the receiver.</typeparam>
    public interface IDuplexTypedMessageReceiver<TResponse, TRequest> : IAttachableDuplexInputChannel
    {
        /// <summary>
        /// Raised when the request message is received.
        /// </summary>
        event EventHandler<TypedRequestReceivedEventArgs<TRequest>> MessageReceived;

        /// <summary>
        /// Raised when the connection was successfully opened
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;

        /// <summary>
        /// Raised when a duplex typed message sender (client) closed the connection.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;

        /// <summary>
        /// Sends the response message.
        /// </summary>
        /// <remarks>
        /// The message will be serialized and sent via duplex output channel to the response receiver (DuplexTypedMessageSender or SyncDuplexTypedMessageSender).
        /// DuplexTypedMessageSender deserializes the message and raises the event ResponseReceived.
        /// In case of SyncDuplexTypedMessageSender it returns from the method SendRequestMessage.
        /// </remarks>
        /// <param name="responseReceiverId">Identifies response receiver which will receive the message.
        /// If responseReceiverId is * the broadcast to all connected response receivers will be sent.</param>
        /// <param name="responseMessage">response message</param>
        void SendResponseMessage(string responseReceiverId, TResponse responseMessage);
    }
}
