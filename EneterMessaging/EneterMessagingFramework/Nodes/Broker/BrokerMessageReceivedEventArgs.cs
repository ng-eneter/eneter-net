/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;

namespace Eneter.Messaging.Nodes.Broker
{
    /// <summary>
    /// Event arguments of the received message from the broker.
    /// </summary>
    public sealed class BrokerMessageReceivedEventArgs : EventArgs
    {
        /// <summary>
        /// Constructs the event from the input parameters.
        /// </summary>
        public BrokerMessageReceivedEventArgs(string messageTypeId, object message)
        {
            MessageTypeId = messageTypeId;
            Message = message;

            ReceivingError = null;
        }

        /// <summary>
        /// Constructs the event from the error detected during receiving of the message.
        /// </summary>
        public BrokerMessageReceivedEventArgs(Exception receivingError)
        {
            MessageTypeId = "";
            Message = "";

            ReceivingError = receivingError;
        }

        /// <summary>
        /// Returns type of the message.
        /// </summary>
        public string MessageTypeId { get; private set; }

        /// <summary>
        /// Returns the message.
        /// </summary>
        public object Message { get; private set; }

        /// <summary>
        /// Returns the error detected during receiving of the message.
        /// </summary>
        public Exception ReceivingError { get; private set; }
    }
}
