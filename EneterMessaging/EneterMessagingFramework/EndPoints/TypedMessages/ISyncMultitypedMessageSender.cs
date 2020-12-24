

using System;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    /// <summary>
    /// Synchronized sender for multiple message types (it waits until the response is received).
    /// </summary>
    public interface ISyncMultitypedMessageSender : IAttachableDuplexOutputChannel
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
        /// Sends request message and returns the response.
        /// </summary>
        /// <typeparam name="TResponse">Type of request message.</typeparam>
        /// <typeparam name="TRequest">Type of response message.</typeparam>
        /// <param name="message">request message</param>
        /// <returns>response message</returns>
        TResponse SendRequestMessage<TResponse, TRequest>(TRequest message);
    }
}
