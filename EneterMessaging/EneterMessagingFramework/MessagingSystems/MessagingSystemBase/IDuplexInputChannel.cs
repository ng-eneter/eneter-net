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
    /// Duplex input channel that can receive messages from the duplex output channel and send response messages.
    /// </summary>
    public interface IDuplexInputChannel
    {
        /// <summary>
        /// The event is invoked when a duplex output channel opened the connection.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;

        /// <summary>
        /// The event is invoked when a duplex output channel closed the connection.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;

        /// <summary>
        /// The event is invoked when a message was received.
        /// </summary>
        event EventHandler<DuplexChannelMessageEventArgs> MessageReceived;

        /// <summary>
        /// Returns id of this duplex input channel.
        /// </summary>
        /// <remarks>
        /// The id represents the 'address' the duplex input channel is listening to.
        /// </remarks>
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
        /// Sends the response message back to the connected IDuplexOutputChannel.
        /// </summary>
        /// <param name="responseReceiverId">Identifies the response receiver. The identifier comes with received messages.</param>
        /// <param name="message">response message</param>
        void SendResponseMessage(string responseReceiverId, object message);

        /// <summary>
        /// Disconnects the response receiver.
        /// </summary>
        /// <param name="responseReceiverId">identifies the response receiver</param>
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
