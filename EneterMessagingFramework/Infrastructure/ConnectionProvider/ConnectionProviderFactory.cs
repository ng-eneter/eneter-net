/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.Infrastructure.ConnectionProvider
{
    /// <summary>
    /// Implements the factory to create the connection provider. The connection provider
    /// helps to attach a channel to a component or to connect two components with a channel.
    /// </summary>
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
