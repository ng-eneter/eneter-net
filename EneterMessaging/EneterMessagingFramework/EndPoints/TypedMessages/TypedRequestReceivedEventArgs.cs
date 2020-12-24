

using System;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    /// <summary>
    /// Event argument used when the typed message is received.
    /// </summary>
    /// <typeparam name="_RequestMessageType"></typeparam>
    public sealed class TypedRequestReceivedEventArgs<_RequestMessageType> : EventArgs
    {
        /// <summary>
        /// Constructs the event.
        /// </summary>
        /// <param name="responseReceiverId">identifies the client where the response can be sent</param>
        /// <param name="senderAddress">address of the message sender. It is null if not applicable for the messaging system.</param>
        /// <param name="requestMessage">message</param>
        public TypedRequestReceivedEventArgs(string responseReceiverId, string senderAddress, _RequestMessageType requestMessage)
        {
            RequestMessage = requestMessage;
            ResponseReceiverId = responseReceiverId;
            SenderAddress = senderAddress;
            ReceivingError = null;
        }

        /// <summary>
        /// Constructs the message from the exception.
        /// </summary>
        /// <param name="responseReceiverId">identifies the client where the response can be sent</param>
        /// <param name="senderAddress">address of the message sender. It is null if not applicable for the messaging system.</param>
        /// <param name="error">error detected during receiving the message</param>
        public TypedRequestReceivedEventArgs(string responseReceiverId, string senderAddress, Exception error)
        {
            RequestMessage = default(_RequestMessageType);
            ResponseReceiverId = responseReceiverId;
            SenderAddress = senderAddress;
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
        /// Returns the address where the sender of the request message is located. (e.g. IP address of the client).
        /// It can be empty string if not applicable for used messaging.
        /// </summary>
        public string SenderAddress { get; private set; }

        /// <summary>
        /// Returns the error detected during receiving of the message.
        /// </summary>
        public Exception ReceivingError { get; private set; }
    }
}
