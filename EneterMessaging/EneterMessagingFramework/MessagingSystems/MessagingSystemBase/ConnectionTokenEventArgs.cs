/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

using System;

namespace Eneter.Messaging.MessagingSystems.MessagingSystemBase
{
    /// <summary>
    /// Event argument used to determine if the response receiver is allowed to establish the connection or not.
    /// </summary>
    public sealed class ConnectionTokenEventArgs : EventArgs
    {
        /// <summary>
        /// Constructs the event argument.
        /// </summary>
        /// <param name="responseReceiverId">Unique logical id identifying the response receiver.</param>
        /// <param name="senderAddress">Address where the response reciever is located. (e.g. IP address of the client)<br/>
        /// Can be empty string if not applicable in used messaging.</param>
        public ConnectionTokenEventArgs(string responseReceiverId, string senderAddress)
        {
            ResponseReceiverId = responseReceiverId;
            SenderAddress = senderAddress;

            // Allow connection by default.
            IsConnectionAllowed = true;
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

        /// <summary>
        /// If set to false the connection with the response receiver shall not be established.
        /// </summary>
        public bool IsConnectionAllowed { get; set; }
    }
}
