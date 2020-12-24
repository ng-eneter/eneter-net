

using System;

namespace Eneter.Messaging.Nodes.LoadBalancer
{
    /// <summary>
    /// Event which is invoked when the receiver is removed from the load balancer pool.
    /// </summary>
    /// <remarks>
    /// The request receiver is removed from the pool if the load balancer cannot open the connection with the receiver.
    /// </remarks>
    sealed public class RequestReceiverRemovedEventArgs : EventArgs
    {
        /// <summary>
        /// Constructs the event.
        /// </summary>
        /// <param name="channelId"></param>
        public RequestReceiverRemovedEventArgs(string channelId)
        {
            ChannelId = channelId;
        }

        /// <summary>
        /// Returns the address of the request receiver.
        /// </summary>
        public string ChannelId { get; private set; }
    }
}