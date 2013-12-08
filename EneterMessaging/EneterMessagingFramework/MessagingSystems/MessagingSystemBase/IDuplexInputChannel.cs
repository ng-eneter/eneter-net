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
    /// Declares the duplex input channel that can receive messages from the duplex output channel and send back response messages.
    /// </summary>
    /// <remarks>
    /// Notice, the duplex input channel works only with duplex output channel and not with output channel.
    /// </remarks>
    public interface IDuplexInputChannel
    {
        /// <summary>
        /// The event is invoked before a duplex output channel opens the connection.
        /// </summary>
        /// <remarks>
        /// The event allows to grant or deny the connection.
        /// E.g. if the IsConnectionAllowed is set to false the connection will not be open.<br/>
        /// This event does not use the dispatcher. Therefore the event can be raised in whatever thread.
        /// </remarks>
        event EventHandler<ConnectionTokenEventArgs> ResponseReceiverConnecting;

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

        /// <summary>
        /// Dispatcher defining the threading model for raising events.
        /// </summary>
        /// <remarks>
        /// Dispatcher is responsible for raising ResponseReceiverConnected, ResponseReceiverDisconnected and MessageReceived events
        /// in desired thread. It allows to specify which threading mechanism/model is used to raise asynchronous events.
        /// E.g. events are queued and raised by one thread. Or e.g. in Silverlight events can be raised in the Silverlight thread.<br/>
        /// The only exception is the event ResponseReceiverConnecting which is not dispatched to any specific thread because
        /// it may set a value which needs to be checked when it returns.
        /// </remarks>
        IDispatcher Dispatcher { get; }
    }
}
