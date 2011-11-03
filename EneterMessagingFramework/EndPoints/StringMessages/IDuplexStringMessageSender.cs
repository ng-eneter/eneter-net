/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using Eneter.Messaging.Infrastructure.Attachable;

namespace Eneter.Messaging.EndPoints.StringMessages
{
    /// <summary>
    /// The interface declares the duplex string message sender.
    /// The duplex sender is able to send text messages and receive text responses.
    /// </summary>
    public interface IDuplexStringMessageSender : IAttachableDuplexOutputChannel
    {
        /// <summary>
        /// The event is invoked when a response message from duplex string message receiver was received.
        /// </summary>
        event EventHandler<StringResponseReceivedEventArgs> ResponseReceived;

        /// <summary>
        /// Sends the message via the attached duplex output channel.
        /// </summary>
        /// <param name="message">message</param>
        void SendMessage(string message);
    }
}
