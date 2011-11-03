/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System.Runtime.CompilerServices;

namespace Eneter.Messaging.MessagingSystems.Composites.MonitoredMessagingComposit
{
    /// <summary>
    /// Network connection monitoring between communicating applications.
    /// </summary>
    /// <remarks>
    /// The network connection monitoring constantly monitors whether the connection is open.
    /// The monitoring is realized by sending 'ping' messages and receiving 'ping' responses.
    /// If the sending of the 'ping' fails or the 'ping' response is not received within the specified
    /// timeout, the connection is considered to be disconnected.
    /// </remarks>
    [CompilerGeneratedAttribute()]
    class NamespaceDoc
    {
    }
}
