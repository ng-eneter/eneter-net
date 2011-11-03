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
    /// The interface declares the composit output channel.
    /// The composit output channel is the output channel that uses underlying output channel
    /// to send messages.
    /// </summary>
    public interface ICompositeOutputChannel : IOutputChannel
    {
        /// <summary>
        /// Returns the underlying output channel.
        /// </summary>
        IOutputChannel UnderlyingOutputChannel { get; }
    }
}
