/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;

namespace Eneter.Messaging.EndPoints.StringMessages
{
    /// <summary>
    /// Declares the event type when the request message is received.
    /// </summary>
    public sealed class StringRequestReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Constructs the event from thr parameters.
        /// </summary>
        /// <param name="requestMessage"></param>
        /// <param name="responseReceiverId"></param>
        /// <param name="senderAddress"></param>
        public StringRequestReceivedEventArgs(string requestMessage, string responseReceiverId, string senderAddress)
        {
            RequestMessage = requestMessage;
            ResponseReceiverId = responseReceiverId;
            SenderAddress = senderAddress;
        }

        /// <summary>
        /// Returns the request message.
        /// </summary>
        public string RequestMessage { get; private set; }

        /// <summary>
        /// Returns the response receiver id.
        /// </summary>
        public string ResponseReceiverId { get; private set; }

        /// <summary>
        /// Returns the address where the sender of the request message is located. (e.g. IP address of the client).
        /// It can be empty string if not applicable for used messaging.
        /// </summary>
        public string SenderAddress { get; private set; }
    }
}
