/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using System;

namespace Eneter.Messaging.Infrastructure.ConnectionProvider
{
    /// <summary>
    /// Implements the factory to create the connection provider. The connection provider
    /// helps to attach a channel to a component or to connect two components with a channel.
    /// </summary>
    [Obsolete("This interface is deprecated and will be removed in the future. Instead of that please create channels using IMessagingSystemFactory and attach them to components via methods AttachDuplexOutputChannel or AttachDuplexInputChannel.")]
    public class ConnectionProviderFactory : IConnectionProviderFactory
    {
        /// <summary>
        /// Creates the connection provider.
        /// </summary>
        /// <param name="messagingSystem">Messaging system the connection provider will use to create channels.</param>
        public IConnectionProvider CreateConnectionProvider(IMessagingSystemFactory messagingSystem)
        {
            using (EneterTrace.Entering())
            {
                return new ConnectionProvider(messagingSystem);
            }
        }

    }
}
