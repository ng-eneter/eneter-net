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
    /// The event when the typed message is received.
    /// </summary>
    /// <typeparam name="_RequestMessageType"></typeparam>
    public sealed class TypedRequestReceivedEventArgs<_RequestMessageType> : EventArgs
    {
        /// <summary>
        /// Constructs the event.
        /// </summary>
        /// <param name="responseReceiverId">identifies the client where the response can be sent</param>
        /// <param name="requestMessage">message</param>
        public TypedRequestReceivedEventArgs(string responseReceiverId, _RequestMessageType requestMessage)
        {
            RequestMessage = requestMessage;
            ResponseReceiverId = responseReceiverId;
            ReceivingError = null;
        }

        /// <summary>
        /// Constructs the message from the exception.
        /// </summary>
        /// <param name="responseReceiverId">identifies the client where the response can be sent</param>
        /// <param name="error">error detected during receiving the message</param>
        public TypedRequestReceivedEventArgs(string responseReceiverId, Exception error)
        {
            RequestMessage = default(_RequestMessageType);
            ResponseReceiverId = responseReceiverId;
            ReceivingError = error;
        }

        /// <summary>
        /// Returns the received message.
        /// </summary>
        public _RequestMessageType RequestMessage { get; private set; }

        /// <summary>
        /// Returns the client identifier where the response can be sent.
        /// </summary>
        public string ResponseReceiverId { get; private set; }

        /// <summary>
        /// Returns the error detected during receiving of the message.
        /// </summary>
        public Exception ReceivingError { get; private set; }
    }
}
