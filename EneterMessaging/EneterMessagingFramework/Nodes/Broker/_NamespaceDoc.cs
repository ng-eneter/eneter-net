/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System.Runtime.CompilerServices;

namespace Eneter.Messaging.Nodes.Broker
{
    /// <summary>
    /// Publish-subscribe scenarios.
    /// </summary>
    /// <remarks>
    /// The broker is the communication component intended for publish-subscribe scenario.
    /// It is the component which allows consumers to subscribe for desired message types
    /// and allows publishers to send a message to subscribed consumers.<br/>
    /// <br/>
    /// When the broker receives a message from a publisher it finds all consumers subscribed to that
    /// message and forwards them the message.
    /// </remarks>
    [CompilerGeneratedAttribute()]
    class NamespaceDoc
    {
    }
}
