

using System;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.EndPoints.StringMessages
{
    /// <summary>
    /// Sender of text messages.
    /// </summary>
    public interface IDuplexStringMessageSender : IAttachableDuplexOutputChannel
    {
        /// <summary>
        /// The event is raised when a response message is received.
        /// </summary>
        event EventHandler<DuplexChannelEventArgs> ConnectionOpened;

        /// <summary>
        /// The event is raised when the connection with the receiver is closed.
        /// </summary>
        event EventHandler<DuplexChannelEventArgs> ConnectionClosed;

        /// <summary>
        /// The event is raised when a response message is received.
        /// </summary>
        event EventHandler<StringResponseReceivedEventArgs> ResponseReceived;

        /// <summary>
        /// Sends the text message to the response receiver.
        /// </summary>
        /// <param name="message">message</param>
        void SendMessage(string message);
    }
}
