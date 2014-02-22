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
    /// Publish-subscribe scenarios (clients can subscribe for notification messages).
    /// </summary>
    /// <remarks>
    /// The broker is intended for publish-subscribe scenarios. Clients can use the broker to publish notification messages or
    /// to subscribe for notification messages.<br/>
    /// The broker works like this:<br/>
    /// The client has some event that wants to notify to everybody who is interested. It sends the message to the broker.
    /// The broker receives the message and forwards it to everybody who is subscribed for such event.
    /// </remarks>
    [CompilerGeneratedAttribute()]
    class NamespaceDoc
    {
    }
}
