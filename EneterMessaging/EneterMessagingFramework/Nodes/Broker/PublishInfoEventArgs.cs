

using System;

namespace Eneter.Messaging.Nodes.Broker
{
    /// <summary>
    /// Event argument used when the broker published a message.
    /// </summary>
    public sealed class PublishInfoEventArgs : EventArgs
    {
        /// <summary>
        /// Constructs the event.
        /// </summary>
        /// <param name="publishedResponseReceiverId">response receiver id of the publisher.</param>
        /// <param name="publishedMessageTypeId">message type which was published.</param>
        /// <param name="publishedMessage">message which was published.</param>
        /// <param name="publishedToSubscribers">number of subscribers to which the message was published.</param>
        public PublishInfoEventArgs(string publishedResponseReceiverId, string publishedMessageTypeId, object publishedMessage, int publishedToSubscribers)
        {
            PublisherResponseReceiverId = publishedResponseReceiverId;
            PublishedMessageTypeId = publishedMessageTypeId;
            PublishedMessage = publishedMessage;
            NumberOfSubscribers = publishedToSubscribers;
        }

        /// <summary>
        /// Returns response receiver id of the publisher.
        /// </summary>
        public string PublisherResponseReceiverId { get; private set; }

        /// <summary>
        /// Returns id of message type which was published.
        /// </summary>
        public string PublishedMessageTypeId { get; private set; }

        /// <summary>
        /// Returns published message.
        /// </summary>
        public object PublishedMessage { get; private set; }

        /// <summary>
        /// Returns number of subscribers to which the message was published.
        /// </summary>
        public int NumberOfSubscribers { get; private set; }
    }
}
