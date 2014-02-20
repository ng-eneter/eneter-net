/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System.Runtime.CompilerServices;

namespace Eneter.Messaging.MessagingSystems.Composites.MessageBus
{
    /// <summary>
    /// Communication via the message bus.
    /// </summary>
    /// <remarks>
    /// The message bus is the component that can be used to expose multiple services from one place.
    /// When a service wants to expose its functionality via the message bus it connects the message bus and registers there.
    /// Then when a client wants to use the service it connects the message bus and asks for the service.
    /// If the requested service is registered the communication between the client and the service is mediated via the message bus.<br/>
    /// </remarks>
    /// <example>
    /// For more details see examples:
    /// <ul>
    ///     <li><see cref="MessageBusMessagingFactory"/> - exposing a simple service via the message bus.</li>
    /// </ul>
    /// </example>
    [CompilerGeneratedAttribute()]
    class NamespaceDoc
    {
    }
}
