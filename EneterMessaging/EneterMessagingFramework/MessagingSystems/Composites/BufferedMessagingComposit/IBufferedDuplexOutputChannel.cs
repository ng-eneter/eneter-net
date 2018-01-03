/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2018
*/

using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using System;

namespace Eneter.Messaging.MessagingSystems.Composites.BufferedMessagingComposit
{
    /// <summary>
    /// Duplex output channel which can work offline.
    /// </summary>
    public interface IBufferedDuplexOutputChannel : IDuplexOutputChannel
    {
        /// <summary>
        /// The event is raised when the connection gets into the online state.
        /// </summary>
        event EventHandler<DuplexChannelEventArgs> ConnectionOnline;

        /// <summary>
        /// The event is raised when the connection gets into the offline state.
        /// </summary>
        event EventHandler<DuplexChannelEventArgs> ConnectionOffline;

        /// <summary>
        /// Returns true if the connection is in the online state.
        /// </summary>
        bool IsOnline { get; }
    }
}
