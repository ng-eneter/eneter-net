/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System;

namespace Eneter.Messaging.MessagingSystems.MessagingSystemBase
{
    /// <summary>
    /// Event argument containing channel id and response receiver id.
    /// </summary>
    public class DuplexChannelEventArgs : EventArgs
    {
        /// <summary>
        /// Constructs the event argument.
        /// </summary>
        /// <param name="channelId">channel id</param>
        /// <param name="responseReceiverId">response receiver id</param>
        public DuplexChannelEventArgs(string channelId, string responseReceiverId)
        {
            ChannelId = channelId;
            ResponseReceiverId = responseReceiverId;
        }

        /// <summary>
        /// Returns the channel id.
        /// </summary>
        public string ChannelId { get; private set; }

        /// <summary>
        /// Returns the response receiver id.
        /// </summary>
        public string ResponseReceiverId { get; private set; }
    }
}
