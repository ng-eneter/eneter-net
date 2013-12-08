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
    /// The event is invoked when a string response message is received.
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
