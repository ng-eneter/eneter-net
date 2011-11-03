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
    /// The event data available when the input channel receives a message.
    /// </summary>
    public sealed class ChannelMessageEventArgs : EventArgs
    {
        /// <summary>
        /// Constructs the event data from the input parameters.
        /// </summary>
        /// <param name="channelId">channel identifier</param>
        /// <param name="message">message</param>
        public ChannelMessageEventArgs(string channelId, object message)
        {
            ChannelId = channelId;
            Message = message;
        }

        /// <summary>
        /// Returns the channel identifier.
        /// </summary>
        public string ChannelId { get; private set; }

        /// <summary>
        /// Returns the message.
        /// </summary>
        public object Message { get; private set; }
    }
}
