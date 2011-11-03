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
    public class StringRequestReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Constructs the event from thr parameters.
        /// </summary>
        /// <param name="requestMessage"></param>
        /// <param name="responseReceiverId"></param>
        public StringRequestReceivedEventArgs(string requestMessage, string responseReceiverId)
        {
            RequestMessage = requestMessage;
            ResponseReceiverId = responseReceiverId;
        }

        /// <summary>
        /// Returns the request message.
        /// </summary>
        public string RequestMessage { get; private set; }

        /// <summary>
        /// Returns the response receiver id.
        /// </summary>
        public string ResponseReceiverId { get; private set; }
    }
}
