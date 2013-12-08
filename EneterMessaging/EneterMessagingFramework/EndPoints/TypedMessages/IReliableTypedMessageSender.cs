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
    /// The interface declares the strongly typed reliable message sender.
    /// The reliable sender can send typed messages and receive typed response messages.
    /// In addition it provides events notifying whether the message was delivered.
    /// The reliable typed message sender can be used only with the reliable typed message receiver.
    /// </summary>
    /// <typeparam name="_ResponseType">type of the response message</typeparam>
    /// <typeparam name="_RequestType">type of the message</typeparam>
    public interface IReliableTypedMessageSender<_ResponseType, _RequestType> : IAttachableDuplexOutputChannel
    {
        /// <summary>
        /// The event is raised when the connection with the receiver is open.
        /// </summary>
        /// <remarks>
        /// Notice, the event is invoked in a thread from the thread pool. Therefore, if you need to manipulate UI,
        /// do not forget to marshal it to the UI thread.
        /// </remarks>
        event EventHandler<DuplexChannelEventArgs> ConnectionOpened;

        /// <summary>
        /// The event is raised when the connection with the receiver is closed.
        /// </summary>
        /// <remarks>
        /// Notice, the event is raised in a thread from the thread pool. Therefore, if you need to manipulate UI,
        /// do not forget to marshal it to the UI thread.
        /// </remarks>
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
        /// Sends the message to the reliable typed message receiver.
        /// </summary>
        /// <param name="message">message to be sent</param>
        /// <returns>id of the message. The id can be then used to check if the message was received.</returns>
        string SendRequestMessage(_RequestType message);
    }
}
