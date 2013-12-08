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
    /// The interface declares the strongly typed duplex message receiver.
    /// The receiver is able to receive messages of the specified type and send back response messages of specified type.
    /// </summary>
    /// <typeparam name="_ResponseType">The type of sending response messages.</typeparam>
    /// <typeparam name="_RequestType">The type of receiving messages.</typeparam>
    public interface IDuplexTypedMessageReceiver<_ResponseType, _RequestType> : IAttachableDuplexInputChannel
    {
        /// <summary>
        /// The event is invoked when the message from a duplex typed message sender was received.
        /// </summary>
        event EventHandler<TypedRequestReceivedEventArgs<_RequestType>> MessageReceived;

        /// <summary>
        /// The event is invoked when a duplex typed message sender opened the connection via its duplex output channel.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;

        /// <summary>
        /// The event is invoked when a duplex typed message sender closed the connection via its duplex output channel.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;

        /// <summary>
        /// Sends the response message back to the duplex typed message sender via the attached duplex input channel.
        /// </summary>
        /// <param name="responseReceiverId">identifies the duplex typed message sender that will receive the response</param>
        /// <param name="responseMessage">response message</param>
        void SendResponseMessage(string responseReceiverId, _ResponseType responseMessage);
    }
}
