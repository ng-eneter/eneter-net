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
    /// Represents the message that was sent.
    /// It stores the message id and the time when the message was sent.
    /// </summary>
    internal class SentMessageItem
    {
        public SentMessageItem(string messageId)
        {
            
            MessageId = messageId;
            SendTime = DateTime.Now;
        }

        public string MessageId { get; private set; }
        public DateTime SendTime { get; private set; }
    }
}
