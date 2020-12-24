

using System;
using System.Collections.Generic;

namespace Eneter.Messaging.Nodes.Broker
{
    /// <summary>
    /// Event argument used when the broker subscribed or unsubscribed a client.
    /// </summary>
    public sealed class SubscribeInfoEventArgs : EventArgs
    {
        /// <summary>
        /// Constructs the event.
        /// </summary>
        /// <param name="subscriberResponseReceiverId">response reciver id of subscriber</param>
        /// <param name="messageTypes">message ids which are subscribed or unsubscribed</param>
        public SubscribeInfoEventArgs(string subscriberResponseReceiverId, IEnumerable<string> messageTypes)
        {
            SubscriberResponseReceiverId = subscriberResponseReceiverId;
            MessageTypeIds = messageTypes;
        }

        /// <summary>
        /// Returns subscriber response receiver id.
        /// </summary>
        public string SubscriberResponseReceiverId { get; private set; }

        /// <summary>
        /// Returns message ids which the client subscribed or unsubscribed.
        /// </summary>
        public IEnumerable<string> MessageTypeIds { get; private set; }
    }
}
