/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;


namespace Eneter.Messaging.EndPoints.TypedMessages
{
    /// <summary>
    /// Sender which confirms if the message was received.
    /// </summary>
    /// <remarks>
    /// It sends request messages of specified type and receives response messages of specified type.
    /// Reliable means it provides events notifying whether the message was delivered.
    /// Messages from ReliableTypedMessageSender can be received only by ReliableTypedMessageReceiver.
    /// </remarks>
    /// <typeparam name="_ResponseType">type of the response message</typeparam>
    /// <typeparam name="_RequestType">type of the message</typeparam>
    public interface IReliableTypedMessageSender<_ResponseType, _RequestType> : IAttachableDuplexOutputChannel
    {
        /// <summary>
        /// The event is raised when the connection with the receiver is open.
        /// </summary>
        event EventHandler<DuplexChannelEventArgs> ConnectionOpened;

        /// <summary>
        /// The event is raised when the connection with the receiver is closed.
        /// </summary>
        event EventHandler<DuplexChannelEventArgs> ConnectionClosed;

        /// <summary>
        /// The event is invoked when the response message is received.
        /// </summary>
        event EventHandler<TypedResponseReceivedEventArgs<_ResponseType>> ResponseReceived;

        /// <summary>
        /// The event is invoked when the message was delivered.
        /// </summary>
        event EventHandler<ReliableMessageIdEventArgs> MessageDelivered;

        /// <summary>
        /// The event is invoked if the event is not delivered within a specified time.
        /// </summary>
        event EventHandler<ReliableMessageIdEventArgs> MessageNotDelivered;
        
        /// <summary>
        /// Sends the message of specified type.
        /// </summary>
        /// <param name="message">message to be sent</param>
        /// <returns>id of the message. The id can be then used to check if the message was received.</returns>
        string SendRequestMessage(_RequestType message);
    }
}
