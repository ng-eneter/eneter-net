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
    /// The event data available when the duplex input channel receives a message.
    /// </summary>
    public sealed class DuplexChannelMessageEventArgs : DuplexChannelEventArgs
    {
        /// <summary>
        /// Constructs the event.
        /// </summary>
        /// <param name="channelId">channel identifier</param>
        /// <param name="message">message</param>
        /// <param name="responseReceiverId">identifies the client receiving response messages</param>
        public DuplexChannelMessageEventArgs(string channelId, object message, string responseReceiverId)
            : base(channelId, responseReceiverId)
        {
            Message = message;
        }

        /// <summary>
        /// Returns the message.
        /// </summary>
        public object Message { get; private set; }
    }
}
