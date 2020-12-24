

using System;

namespace Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase
{
    /// <summary>
    /// Declares the input connector which provides a basic low-level listening.
    /// </summary>
    internal interface IInputConnector
    {
        /// <summary>
        /// Starts listening to messages.
        /// </summary>
        /// <param name="messageHandler">handler processing incoming messages. If it returns true the connection stays
        /// open and listener can loop for a next messages. If it returns false the listener shall not loop for the
        /// next message.</param>
        void StartListening(Action<MessageContext> messageHandler);

        /// <summary>
        /// Stops listening to messages.
        /// </summary>
        void StopListening();

        /// <summary>
        /// Returns true if the listening is running.
        /// </summary>
        bool IsListening { get; }

        void SendResponseMessage(string outputConnectorAddress, object message);

        void SendBroadcast(object message);

        void CloseConnection(string outputConnectorAddress);
    }
}
