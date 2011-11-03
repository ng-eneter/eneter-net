/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;

namespace Eneter.Messaging.MessagingSystems.MessagingSystemBase
{
    /// <summary>
    /// Declares the output channel that can send messages to the input channel.
    /// </summary>
    /// <remarks>
    /// Notice, the output channel can send messages only to the input channel and not to the duplex input channel.
    /// </remarks>
    public interface IOutputChannel
    {
        /// <summary>
        /// Returns the id representing the address where messages are sent.
        /// </summary>
        string ChannelId { get; }

        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <remarks>
        /// Notice, there is a limitation for the Silverlight platform using HTTP.
        /// If the message is sent via HTTP from the main Silverlight thread, then in case of a failure, the exception is not thrown.
        /// Therefore, it is recommended to execute this method in a different thread.
        /// </remarks>
        /// <param name="message">serialized message</param>
        /// <exception cref="Exception">Any exception thrown during sending of a message. E.g. if sending via TCP fails.</exception>
        void SendMessage(object message);
    }
}
