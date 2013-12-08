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
    /// Event argument containing parameters of the communication.
    /// </summary>
    public class DuplexChannelEventArgs : EventArgs
    {
        /// <summary>
        /// Constructs the event argument.
        /// </summary>
        /// <param name="channelId">Identifies the receiver of request messages. (e.g. tcp://127.0.0.1:8090/)</param>
        /// <param name="responseReceiverId">Unique logical id identifying the receiver of response messages.</param>
        /// <param name="senderAddress">Address where the sender of the request message is located. (e.g. IP address of the client)<br/>
        /// Can be empty string if not applicable in used messaging.</param>
        public DuplexChannelEventArgs(string channelId, string responseReceiverId, string senderAddress)
        {
            ChannelId = channelId;
            ResponseReceiverId = responseReceiverId;
            SenderAddress = senderAddress;
        }

        /// <summary>
        /// Returns the channel id identifying the receiver of request messages. (e.g. tcp://127.0.0.1:8090/).
        /// </summary>
        public string ChannelId { get; private set; }

        /// <summary>
        /// Returns the unique logical id identifying the receiver of response messages.
        /// </summary>
        public string ResponseReceiverId { get; private set; }

        /// <summary>
        /// Returns the address where the sender of the request message is located. (e.g. IP address of the client).
        /// It can be empty string if not applicable for used messaging.
        /// </summary>
        public string SenderAddress { get; private set; }
    }
}
