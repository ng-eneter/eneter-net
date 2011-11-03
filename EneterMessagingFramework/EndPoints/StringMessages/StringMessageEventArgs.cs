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
    /// The string message received event.
    /// </summary>
    public sealed class StringMessageEventArgs : EventArgs
    {
        /// <summary>
        /// Constructs the event.
        /// </summary>
        public StringMessageEventArgs(string message)
        {
            Message = message;
        }

        /// <summary>
        /// Returns the received string message.
        /// </summary>
        public string Message { get; private set; }
    }
}
