/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.Nodes.ChannelWrapper
{
    /// <summary>
    /// Declares the factory for creating channel wrappers and and channel unwrappers.
    /// </summary>
    public interface IChannelWrapperFactory
    {
        /// <summary>
        /// Creates the channel wrapper.
        /// </summary>
        /// <returns></returns>
        IChannelWrapper CreateChannelWrapper();

        /// <summary>
        /// Creates the channel unwrapper.
        /// </summary>
        /// <returns></returns>
        IChannelUnwrapper CreateChannelUnwrapper(IMessagingSystemFactory outputMessagingSystem);

        /// <summary>
        /// Creates the duplex channel wrapper.
        /// </summary>
        /// <returns></returns>
        IDuplexChannelWrapper CreateDuplexChannelWrapper();

        /// <summary>
        /// Creates the duplex channel unwrapper.
        /// </summary>
        /// <returns></returns>
        IDuplexChannelUnwrapper CreateDuplexChannelUnwrapper(IMessagingSystemFactory outputMessagingSystem);
    }
}
