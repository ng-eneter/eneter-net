/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;

namespace Eneter.Messaging.MessagingSystems.MessagingSystemBase
{
    /// <summary>
    /// Declares the duplex input channel that can receive messages from the duplex output channel and send back response messages.
    /// </summary>
    /// <remarks>
    /// Notice, the duplex input channel works only with duplex output channel and not with output channel.
    /// </remarks>
    public interface IDuplexInputChannel
    {
        /// <summary>
        /// The event is invoked when a message was received.
        /// </summary>
        event EventHandler<DuplexChannelMessageEventArgs> MessageReceived;

        /// <summary>
        /// The event is invoked when a duplex output channel opened the connection.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;

        /// <summary>
        /// The event is invoked when a duplex output channel closed the connection.
        /// </summary>
        event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;

        /// <summary>
        /// Returns id of this duplex input channel.
        /// The id represents the 'address' the duplex input channel is listening to.
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
    }
}
