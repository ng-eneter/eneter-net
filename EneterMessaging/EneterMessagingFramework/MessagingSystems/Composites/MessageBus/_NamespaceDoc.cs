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
    /// Extension for communication via the message bus.
    /// </summary>
    /// <remarks>
    /// The message bus is the component that can be used to expose multiple services from one place.
    /// It means when a service wants to expose its functionality it connects the message bus and registers its service id.
    /// Then when a client wants to use the service it connects the message bus and asks for the service using the service id.
    /// Message bus is then responsible to establish the connection between the client and the service.<br/>
    /// This extension hides the communication is running via the message bus. For communicating parts it looks as if they
    /// communicate directly.
    /// <example>
    /// For more details see examples:
    /// <ul>
    ///     <li><see cref="MessageBusMessagingFactory"/> - exposing a simple service via the message bus.</li>
    /// </ul>
    /// </example>
    /// </remarks>
    [CompilerGeneratedAttribute()]
    class NamespaceDoc
    {
    }
}
