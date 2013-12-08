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
    /// Monitoring the connection between communicating applications.
    /// </summary>
    /// <remarks>
    /// The monitoring is realized by sending 'ping' messages and receiving 'ping' responses.
    /// If the sending of the 'ping' fails or the 'ping' response is not received within the specified
    /// time, the connection is considered to be broken.
    /// </remarks>
    [CompilerGeneratedAttribute()]
    class NamespaceDoc
    {
    }
}
