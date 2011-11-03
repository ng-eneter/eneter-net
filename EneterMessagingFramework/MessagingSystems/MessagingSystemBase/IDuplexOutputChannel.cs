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
    /// Declares the duplex output channel that can send messages to the duplex input channel and receive response messages.
    /// </summary>
    /// <remarks>
    /// Notice, the duplex output channel works only with duplex input channel and not with input channel.
    /// </remarks>
    public interface IDuplexOutputChannel
    {
        /// <summary>
        /// The event is invoked when a response message was received.
        /// </summary>
        /// <remarks>
        /// Notice, this event is invoked in a different thread. The exception is only the Synchronous messaging that
        /// invokes this event in the thread calling the method SendResponseMessage in <see cref="IDuplexInputChannel"/>.
        /// Also, in Silverlight (and Windows Phone 7), http and tcp messagings have the possibility
        /// to choose if the thread shall be the main Silverlight thread.
        /// </remarks>
        event EventHandler<DuplexChannelMessageEventArgs> ResponseMessageReceived;

        /// <summary>
        /// The event is invoked when the connection with the duplex input channel was opened.
        /// </summary>
        /// <remarks>
        /// Notice, the event is invoked in a thread from the thread pool. Therefore, if you need to manipulate UI,
        /// do not forget to marshal it to the UI thread.
        /// </remarks>
        event EventHandler<DuplexChannelEventArgs> ConnectionOpened;

        /// <summary>
        /// The event is invoked when the connection with the duplex input channel was closed.
        /// </summary>
        /// <remarks>
        /// Notice, the event is invoked in a thread from the thread pool. Therefore, if you need to manipulate UI,
        /// do not forget to marshal it to the UI thread.
        /// </remarks>
        event EventHandler<DuplexChannelEventArgs> ConnectionClosed;

        /// <summary>
        /// Returns the id of the duplex input channel where messages are sent.
        /// It represents the address where messages are sent.
        /// </summary>
        /// <remarks>
        /// The channel id represents the communication address. The syntax of the channel id depends on the chosen
        /// communication. If the messaging is based on http, the address would be e.g.: http://127.0.0.1/Something/ or
        /// http://127.0.0.1:7345/Something/. If the communication is based on tcp, the address would be e.g.: tcp://127.0.0.1:7435/.
        /// For the named pipe, e.g. net.pipe://127.0.0.1/SomePipeName/.
        /// </remarks>
        string ChannelId { get; }

        /// <summary>
        /// Returns response receiving id of the duplex output channel.
        /// </summary>
        /// <remarks>
        /// The response receiver id is a unique identifier used by the duplex input channel to recognize
        /// connected duplex output channels.
        /// </remarks>
        string ResponseReceiverId { get; }

        /// <summary>
        /// Sends the message to the address represented by ChannelId.
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
    }
}
