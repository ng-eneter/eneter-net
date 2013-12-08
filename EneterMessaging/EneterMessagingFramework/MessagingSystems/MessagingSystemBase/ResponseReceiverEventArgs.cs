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
    /// Event argument representing the response receiver on the service site.
    /// </summary>
    /// <remarks>
    /// This event argument is typically used e.g. when the client opened/closed connection. 
    /// </remarks>
    public sealed class ResponseReceiverEventArgs : EventArgs
    {
        /// <summary>
        /// Constructs the event argument.
        /// </summary>
        /// <param name="responseReceiverId">Unique logical id identifying the receiver of response messages.</param>
        /// <param name="senderAddress">Address where the sender of the request message is located. (e.g. IP address of the client)<br/>
        /// Can be empty string if not applicable in used messaging.</param>
        public ResponseReceiverEventArgs(string responseReceiverId, string senderAddress)
        {
            ResponseReceiverId = responseReceiverId;
            SenderAddress = senderAddress;
        }

        /// <summary>
        /// Returns the unique logical id identifying the receiver of response messages.
        /// </summary>
        /// <remarks>
        /// This id identifies who receives the response message on the client side.
        /// </remarks>
        public string ResponseReceiverId { get; private set; }

        /// <summary>
        /// Returns the address where the sender of the request message is located. (e.g. IP address of the client).
        /// It can be empty string if not applicable for used messaging.
        /// </summary>
        public string SenderAddress { get; private set; }
    }
}
