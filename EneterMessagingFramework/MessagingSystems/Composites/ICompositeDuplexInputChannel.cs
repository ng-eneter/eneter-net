/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.Composites
{
    /// <summary>
    /// The interface declares the composit duplex input channel.
    /// The composit duplex input channel is the duplex input channel that uses underlying duplex input channel
    /// to receive messages and send respones messages.
    /// </summary>
    public interface ICompositeDuplexInputChannel : IDuplexInputChannel
    {
        /// <summary>
        /// Returns the underlying duplex input channel.
        /// </summary>
        IDuplexInputChannel UnderlyingDuplexInputChannel { get; }
    }
}
