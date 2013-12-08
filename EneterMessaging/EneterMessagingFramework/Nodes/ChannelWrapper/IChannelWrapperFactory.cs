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
        /// Creates the duplex channel wrapper.
        /// </summary>
        /// <returns></returns>
        IDuplexChannelWrapper CreateDuplexChannelWrapper();

        /// <summary>
        /// Creates the duplex channel unwrapper.
        /// </summary>
        /// <param name="outputMessagingSystem">Messaging used to create output channels where unwrapped messages will be sent.</param>
        /// <returns></returns>
        IDuplexChannelUnwrapper CreateDuplexChannelUnwrapper(IMessagingSystemFactory outputMessagingSystem);
    }
}
