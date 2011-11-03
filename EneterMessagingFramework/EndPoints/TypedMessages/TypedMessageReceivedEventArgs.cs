/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    /// <summary>
    /// The typed message received event.
    /// </summary>
    public sealed class TypedMessageReceivedEventArgs<_MessageData> : EventArgs
    {
        /// <summary>
        /// Constructs the event.
        /// </summary>
        public TypedMessageReceivedEventArgs(_MessageData messageData)
        {
            MessageData = messageData;
            ReceivingError = null;
        }

        /// <summary>
        /// Constructs the event from the given error message.
        /// </summary>
        /// <param name="error"></param>
        public TypedMessageReceivedEventArgs(Exception error)
        {
            MessageData = default(_MessageData);
            ReceivingError = error;
        }

        /// <summary>
        /// Returns the received message.
        /// </summary>
        public _MessageData MessageData { get; private set; }

        /// <summary>
        /// Returns the error detected during receiving of the message.
        /// E.g. during the deserialization of the message.
        /// </summary>
        public Exception ReceivingError { get; private set; }
    }
}
