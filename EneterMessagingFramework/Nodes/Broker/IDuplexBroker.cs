/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using Eneter.Messaging.Infrastructure.Attachable;

namespace Eneter.Messaging.Nodes.Broker
{
    /// <summary>
    /// The interface declares the broker.
    /// The broker receives messages and forwards them to subscribed clients.
    /// </summary>
    public interface IDuplexBroker : IAttachableDuplexInputChannel
    {
        
    }
}
