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
    /// Declares the duplex message sender which can send text messages and receive text responses.
    /// </summary>
    public interface IDuplexStringMessageSender : IAttachableDuplexOutputChannel
    {
        /// <summary>
        /// The event is raised when a response message is received.
        /// </summary>
        event EventHandler<DuplexChannelEventArgs> ConnectionOpened;

        /// <summary>
        /// The event is raised when the connection with the receiver is closed.
        /// </summary>
        /// <remarks>
        /// Notice, the event is invoked in a thread from the thread pool. Therefore, if you need to manipulate UI,
        /// do not forget to marshal it to the UI thread.
        /// </remarks>
        event EventHandler<DuplexChannelEventArgs> ConnectionClosed;

        /// <summary>
        /// The event is raised when a response message from duplex string message receiver was received.
        /// </summary>
        event EventHandler<StringResponseReceivedEventArgs> ResponseReceived;

        /// <summary>
        /// Sends the message via the attached duplex output channel.
        /// </summary>
        /// <param name="message">message</param>
        void SendMessage(string message);
    }
}
