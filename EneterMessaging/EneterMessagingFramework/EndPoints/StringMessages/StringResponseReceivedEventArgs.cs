﻿

using System;

namespace Eneter.Messaging.EndPoints.StringMessages
{
    /// <summary>
    /// Event type for text response message is received.
    /// </summary>
    public sealed class StringResponseReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Constructs the event.
        /// </summary>
        /// <param name="responseMessage">message</param>
        public StringResponseReceivedEventArgs(string responseMessage)
        {
            ResponseMessage = responseMessage;
        }

        /// <summary>
        /// Returns the response message.
        /// </summary>
        public string ResponseMessage { get; private set; }
    }
}
