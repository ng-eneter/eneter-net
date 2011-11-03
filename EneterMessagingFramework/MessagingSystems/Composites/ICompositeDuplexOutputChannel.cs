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
    /// The interface declares the composit duplex output channel.
    /// The composit duplex output channel is the duplex output channel that uses underlying duplex output channel
    /// to send messages and receive response mssages.
    /// </summary>
    public interface ICompositeDuplexOutputChannel : IDuplexOutputChannel
    {
        /// <summary>
        /// Returns the underlying duplex output channel.
        /// </summary>
        IDuplexOutputChannel UnderlyingDuplexOutputChannel { get; }
    }
}
