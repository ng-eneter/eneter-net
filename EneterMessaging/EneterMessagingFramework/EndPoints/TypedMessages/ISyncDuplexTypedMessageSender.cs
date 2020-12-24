


using System;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    /// <summary>
    /// Synchronized sender for one specified message type (it waits until the response is received).
    /// </summary>
    /// <remarks>
    /// Message sender which sends request messages of specified type and receive response messages of specified type.
    /// Synchronous means when the message is sent it waits until the response message is received.
    /// If the waiting for the response message exceeds the specified timeout the TimeoutException is thrown.
    /// </remarks>
    /// <typeparam name="TResponse">Type of response message.</typeparam>
    /// <typeparam name="TRequest">Type of request message.</typeparam>
    public interface ISyncDuplexTypedMessageSender<TResponse, TRequest> : IAttachableDuplexOutputChannel
    {
        /// <summary>
        /// Raised when the connection with the receiver is open.
        /// </summary>
        event EventHandler<DuplexChannelEventArgs> ConnectionOpened;

        /// <summary>
        /// Raised when the service closed the connection with the client.
        /// </summary>
        /// <remarks>
        /// The event is raised only if the service closes the connection with the client.
        /// It is not raised if the client closed the connection by IDuplexOutputChannel.CloseConnection().
        /// </remarks>
        event EventHandler<DuplexChannelEventArgs> ConnectionClosed;

        /// <summary>
        /// Sends the request message and returns the response.
        /// </summary>
        /// <remarks>
        /// It waits until the response message is received. If waiting for the response exceeds the specified timeout
        /// TimeoutException is thrown.
        /// </remarks>
        /// <param name="message">request message</param>
        /// <returns>response message</returns>
        TResponse SendRequestMessage(TRequest message);
    }
}

