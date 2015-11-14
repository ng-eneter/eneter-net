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
    /// Duplex output channel which can send messages to the duplex input channel and receive response messages.
    /// </summary>
    public interface IDuplexOutputChannel
    {
        /// <summary>
        /// The event is invoked when the connection with the duplex input channel was opened.
        /// </summary>
        event EventHandler<DuplexChannelEventArgs> ConnectionOpened;

        /// <summary>
        /// The event is invoked when the connection with the duplex input channel was closed.
        /// </summary>
        event EventHandler<DuplexChannelEventArgs> ConnectionClosed;

        /// <summary>
        /// The event is invoked when a response message was received.
        /// </summary>
        event EventHandler<DuplexChannelMessageEventArgs> ResponseMessageReceived;

        /// <summary>
        /// Returns the address of the duplex input channel which shall be connected by this duplex output channel.
        /// </summary>
        /// <remarks>
        /// The channel id represents the communication address. The syntax of the channel id depends on the chosen
        /// communication. If the messaging is based on http, the address would be e.g.: http://127.0.0.1/Something/ or
        /// http://127.0.0.1:7345/Something/. If the communication is based on tcp, the address would be e.g.: tcp://127.0.0.1:7435/.
        /// For the named pipe, e.g. net.pipe://127.0.0.1/SomePipeName/.
        /// </remarks>
        string ChannelId { get; }

        /// <summary>
        /// Returns response unique id of this duplex output channel.
        /// </summary>
        string ResponseReceiverId { get; }

        /// <summary>
        /// Sends the message to the duplex input channel.
        /// </summary>
        /// <remarks>
        /// Notice, there is a limitation for the Silverlight platform using HTTP.
        /// If the message is sent via HTTP from the main Silverlight thread, then in case of a failure, the exception is not thrown.
        /// Therefore, it is recommended to execute this method in a different thread.
        /// </remarks>
        /// <exception cref="InvalidOperationException">If the connection is not open.</exception>
        /// <exception cref="Exception">Any exception thrown during sending of a message. E.g. if sending via TCP fails.</exception>
        void SendMessage(object message);

        /// <summary>
        /// Opens the connection with the duplex input channel.
        /// </summary>
        /// <remarks>
        /// Notice, there is a limitation for the Silverlight platform using HTTP.
        /// If the message is sent via HTTP from the main Silverlight thread, then in case of a failure, the exception is not thrown.
        /// Therefore, it is recommended to execute this method in a different thread.
        /// </remarks>
        /// <exception cref="InvalidOperationException">If the connection is already open.</exception>
        void OpenConnection();

        /// <summary>
        /// Closes the connection with the duplex input channel.
        /// </summary>
        /// <remarks>
        /// Notice, there is a limitation for the Silverlight platform.
        /// If this method is executed in the main Silverlight thread, then in case of a failure the exception will not be propagated.
        /// It is recommended to execute this method in a different thread.
        /// </remarks>
        void CloseConnection();

        /// <summary>
        /// Returns true if the duplex output channel is connected to the duplex input channel and listens to response messages.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Returns dispatcher that defines the threading model for raising events.
        /// </summary>
        /// <remarks>
        /// Dispatcher is responsible for raising ConnectionOpened, ConnectionClosed and ResponseMessageReceived events
        /// according to desired thread model.
        /// E.g. events are queued and raised by one particular thread.
        /// </remarks>
        IThreadDispatcher Dispatcher { get; }
    }
}
