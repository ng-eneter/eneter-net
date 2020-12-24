

using System;
using System.Collections.Generic;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    /// <summary>
    /// Sender for multiple message types.
    /// </summary>
    /// <remarks>
    /// This is a client component which can send and receive messages of multiple types.
    /// </remarks>
    public interface IMultiTypedMessageSender : IAttachableDuplexOutputChannel
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
        /// Registers response message handler for specified message type.
        /// </summary>
        /// <remarks>
        /// If the response message of the specified type is received the handler will be called to process it.
        /// </remarks>
        /// <typeparam name="T">data type of the response message which will be handled by the handler</typeparam>
        /// <param name="handler">handler which will be called if the response message of specified type is received</param>
        void RegisterResponseMessageReceiver<T>(EventHandler<TypedResponseReceivedEventArgs<T>> handler);

        /// <summary>
        /// Unregisters the response message handler for the specified message type.
        /// </summary>
        /// <typeparam name="T">data type of the resposne message for which the handler shall be unregistered</typeparam>
        void UnregisterResponseMessageReceiver<T>();

        /// <summary>
        /// Returns the list of registered response message types which can be received.
        /// </summary>
        IEnumerable<Type> RegisteredResponseMessageTypes { get; }

        /// <summary>
        /// Sends request message.
        /// </summary>
        /// <remarks>
        /// The message of the specified type will be serialized and sent to the receiver.
        /// If the receiver has registered a handler for this message type then the handler will be called to process the message.
        /// </remarks>
        /// <typeparam name="TRequestMessage">Type of the message.</typeparam>
        /// <param name="message">request message</param>
        void SendRequestMessage<TRequestMessage>(TRequestMessage message);
    }
}
