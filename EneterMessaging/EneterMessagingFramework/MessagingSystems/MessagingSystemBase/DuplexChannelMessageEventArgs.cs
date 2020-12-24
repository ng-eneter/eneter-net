


namespace Eneter.Messaging.MessagingSystems.MessagingSystemBase
{
    /// <summary>
    /// Event argument used to notify that duplex input channel received a message.
    /// </summary>
    public sealed class DuplexChannelMessageEventArgs : DuplexChannelEventArgs
    {
        /// <summary>
        /// Constructs the event.
        /// </summary>
        /// <param name="channelId">Identifies the receiver of request messages. (e.g. tcp://127.0.0.1:8090/)</param>
        /// <param name="message">message</param>
        /// <param name="responseReceiverId">Unique logical id identifying the receiver of response messages.</param>
        /// <param name="senderAddress">Address where the sender of the request message is located. (e.g. IP address of the client)<br/>
        /// Can be empty string if not applicable in used messaging.</param>
        public DuplexChannelMessageEventArgs(string channelId, object message, string responseReceiverId, string senderAddress)
            : base(channelId, responseReceiverId, senderAddress)
        {
            Message = message;
        }

        /// <summary>
        /// Returns the message.
        /// </summary>
        public object Message { get; private set; }
    }
}
