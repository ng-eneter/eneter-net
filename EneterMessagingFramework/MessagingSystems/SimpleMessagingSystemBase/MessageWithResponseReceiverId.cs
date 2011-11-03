/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/


namespace Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase
{
    /// <summary>
    /// Represents the message that contains the sender id too.
    /// It is used for request-response messaging where the sender id is used by the message
    /// receiver to send back the response messages.
    /// </summary>
    internal class MessageWithResponseReceiverId
    {
        public MessageWithResponseReceiverId(string senderId, object message)
        {
            SenderId = senderId;
            Message = message;
        }

        /// <summary>
        /// Id of the message sender. The message receiver can use the sender id to send back the response messages.
        /// </summary>
        public string SenderId { get; private set; }

        /// <summary>
        /// The content of the message.
        /// </summary>
        public object Message { get; private set; }
    }
}
