

using System;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    /// <summary>
    /// Event argument used when a typed response message is received.
    /// </summary>
    /// <typeparam name="_ResponseMessageType">message type</typeparam>
    public sealed class TypedResponseReceivedEventArgs<_ResponseMessageType> : EventArgs
    {
        /// <summary>
        /// Constructs the event.
        /// </summary>
        /// <param name="responseMessage">response message</param>
        public TypedResponseReceivedEventArgs(_ResponseMessageType responseMessage)
        {
            ResponseMessage = responseMessage;
            ReceivingError = null;
        }

        /// <summary>
        /// Constructs the event from the exception detected during receiving the response message.
        /// </summary>
        /// <param name="error"></param>
        public TypedResponseReceivedEventArgs(Exception error)
        {
            ResponseMessage = default(_ResponseMessageType);
            ReceivingError = error;
        }

        /// <summary>
        /// Returns the message.
        /// </summary>
        public _ResponseMessageType ResponseMessage { get; private set; }

        /// <summary>
        /// Returns an exception detected during receiving the response message.
        /// </summary>
        public Exception ReceivingError { get; private set; }
    }
}
