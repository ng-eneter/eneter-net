/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System;

namespace Eneter.Messaging.MessagingSystems.Composites.ReliableMessagingComposit
{
    /// <summary>
    /// The event arguments used for the notification whether the message was delivered or not delivered.
    /// </summary>
    public class MessageIdEventArgs : EventArgs
    {
        /// <summary>
        /// Constructs the event arguments.
        /// </summary>
        /// <param name="messageId">id of the message</param>
        public MessageIdEventArgs(string messageId)
        {
            MessageId = messageId;
        }

        /// <summary>
        /// Returns id of the message.
        /// </summary>
        public string MessageId { get; private set; }
    }
}
