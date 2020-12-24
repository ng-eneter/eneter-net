

using System;

namespace Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase
{
    /// <summary>
    /// The interface declares the default functionality to send and receive messages.
    /// </summary>
    internal interface IMessagingProvider
    {
        /// <summary>
        /// Sends the message to desired receiver.
        /// </summary>
        /// <param name="receiverId"></param>
        /// <param name="message"></param>
        void SendMessage(string receiverId, object message);

        /// <summary>
        /// Registers the method handling the message.
        /// </summary>
        /// <param name="receiverId"></param>
        /// <param name="messageHandler"></param>
        void RegisterMessageHandler(string receiverId, Action<object> messageHandler);

        /// <summary>
        /// Unregisters the handler.
        /// </summary>
        /// <param name="receiverId"></param>
        void UnregisterMessageHandler(string receiverId);
    }
}
