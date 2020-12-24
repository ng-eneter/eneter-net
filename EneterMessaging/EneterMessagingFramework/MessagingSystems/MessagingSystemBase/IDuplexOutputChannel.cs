

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
        /// The event is raised when the connection with the duplex input channel was opened.
        /// </summary>
        event EventHandler<DuplexChannelEventArgs> ConnectionOpened;

        /// <summary>
        /// The event is raised when the connection was closed from the input channel or the it was closed due to a broken connection.
        /// </summary>
        /// <remarks>
        /// The event is not raised if the connection was closed by the output channel by calling CloseConnection().
        /// </remarks>
        event EventHandler<DuplexChannelEventArgs> ConnectionClosed;

        /// <summary>
        /// The event is raised when a response message was received.
        /// </summary>
        event EventHandler<DuplexChannelMessageEventArgs> ResponseMessageReceived;

        /// <summary>
        /// Returns the address of the input channel.
        /// </summary>
        /// <remarks>
        /// The channel id represents the communication address. The syntax of the channel id depends on the chosen
        /// communication. If the messaging is based on http, the address would be e.g.: http://127.0.0.1/Something/ or
        /// http://127.0.0.1:7345/Something/. If the communication is based on tcp, the address would be e.g.: tcp://127.0.0.1:7435/.
        /// For the named pipe, e.g. net.pipe://127.0.0.1/SomePipeName/.
        /// </remarks>
        string ChannelId { get; }

        /// <summary>
        /// Returns the unique identifier of this output channel.
        /// </summary>
        string ResponseReceiverId { get; }

        /// <summary>
        /// Sends the message to the input channel.
        /// </summary>
        /// <param name="message">message to be sent. It can be String or byte[] or some other type depending on used protocol formatter.</param>
        /// <exception cref="InvalidOperationException">If the connection is not open.</exception>
        /// <exception cref="Exception">Any exception thrown during sending of a message. E.g. if sending via TCP fails.</exception>
        void SendMessage(object message);

        /// <summary>
        /// Opens the connection with the duplex input channel.
        /// </summary>
        /// <exception cref="InvalidOperationException">If the connection is already open.</exception>
        void OpenConnection();

        /// <summary>
        /// Closes the connection with the duplex input channel.
        /// </summary>
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
