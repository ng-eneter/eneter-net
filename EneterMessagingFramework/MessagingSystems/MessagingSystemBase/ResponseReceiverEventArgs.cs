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
    /// The event data representing the response receiver id.
    /// The event is used for the communication between the duplex output channel and duplex input channel
    /// to identify where to send response messages.
    /// </summary>
    public sealed class ResponseReceiverEventArgs : EventArgs
    {
        /// <summary>
        /// Constructs the event from the input parameters.
        /// </summary>
        /// <param name="responseReceiverId">identifies the response message receiver</param>
        public ResponseReceiverEventArgs(string responseReceiverId)
        {
            ResponseReceiverId = responseReceiverId;
        }

        /// <summary>
        /// Returns response message receiver.
        /// </summary>
        public string ResponseReceiverId { get; private set; }
    }
}
