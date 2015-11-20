/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.MessagingSystems.MessagingSystemBase
{
    /// <summary>
    /// Duplex input channel which can receive messages from the duplex output channel and send response messages.
    /// </summary>
    public interface IDuplexInputChannel
    {
        /// <summary>
        /// The event is raised when an output channel opened the connection.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;

        /// <summary>
        /// The event is raised when an output channel closed the connection.
        /// </summary>
        /// <remarks>
        /// The event is not raised when the connection was closed by the input channel.
        /// </remarks>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;

        /// <summary>
        /// The event is raised when a message was received.
        /// </summary>
        event EventHandler<DuplexChannelMessageEventArgs> MessageReceived;

        /// <summary>
        /// Returns address of this duplex input channel.
        /// </summary>
        string ChannelId { get; }

        /// <summary>
        /// Starts listening to messages.
        /// </summary>
        void StartListening();

        /// <summary>
        /// Stops listening to messages.
        /// </summary>
        void StopListening();

        /// <summary>
        /// Returns true if the duplex input channel is listening.
        /// </summary>
        bool IsListening { get; }

        /// <summary>
        /// Sends a message to a connected output channel.
        /// </summary>
        /// <param name="responseReceiverId">Identifies the connected output channel to which the message shall be sent.
        /// If the value is * then the input channel sends the message to all connected output channels.
        /// <example>
        /// Sends message to all connected output channels.
        /// <code>
        /// anInputChannel.SendResponseMessage("*", "Hello");
        /// </code>
        /// </example>
        /// </param>
        /// <param name="message">response message</param>
        void SendResponseMessage(string responseReceiverId, object message);

        /// <summary>
        /// Disconnects the output channel.
        /// </summary>
        /// <param name="responseReceiverId">Identifies output channel which shall be disconnected.</param>
        void DisconnectResponseReceiver(string responseReceiverId);

        /// <summary>
        /// Returns dispatcher that defines the threading model for raising events.
        /// </summary>
        /// <remarks>
        /// Dispatcher is responsible for raising ResponseReceiverConnected, ResponseReceiverDisconnected and MessageReceived events
        /// according to desired thread model.
        /// E.g. events are queued and raised by one particular thread.
        /// </remarks>
        IThreadDispatcher Dispatcher { get; }
    }
}
