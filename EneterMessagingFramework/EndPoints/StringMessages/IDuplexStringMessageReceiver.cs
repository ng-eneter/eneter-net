/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.EndPoints.StringMessages
{
    /// <summary>
    /// The interface declares the reliable string message receiver.
    /// The reliable string message receiver can receiver string messages and response string messages.
    /// </summary>
    public interface IDuplexStringMessageReceiver : IAttachableDuplexInputChannel
    {
        /// <summary>
        /// The event is invoked when the message was received.
        /// </summary>
        event EventHandler<StringRequestReceivedEventArgs> RequestReceived;

        /// <summary>
        /// The event is invoked when a duplex string message sender opened the connection.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;

        /// <summary>
        /// The event is invoked when a duplex string message sender closed the connection.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;

        /// <summary>
        /// Sends the response message back to the duplex string message sender.
        /// </summary>
        /// <param name="responseReceiverId">identifies the duplex string message sender that will receive the response</param>
        /// <param name="responseMessage">response message</param>
        void SendResponseMessage(string responseReceiverId, string responseMessage);
    }
}
