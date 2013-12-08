/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.Infrastructure.ConnectionProvider
{
    /// <summary>
    /// The interface declares the factory to create the connection provider. The connection provider
    /// helps to attach a channel to a component or to connect two components with a channel.
    /// </summary>
    public interface IConnectionProviderFactory
    {
        /// <summary>
        /// Creates the connenction provider.
        /// </summary>
        /// <param name="messagingSystem">Messaging system the connection provider will use to create channels.</param>
        IConnectionProvider CreateConnectionProvider(IMessagingSystemFactory messagingSystem);
    }
}
