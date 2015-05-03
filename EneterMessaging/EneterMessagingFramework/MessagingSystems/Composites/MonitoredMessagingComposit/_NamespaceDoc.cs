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
    /// Extension providing monitoring the connection.
    /// </summary>
    /// <remarks>
    /// The monitoring is realized by sending and receiving 'ping' messages within the specified time.
    /// If the sending of the 'ping' fails or the 'ping' response is not received within the specified
    /// time the connection is considered to be broken.
    /// </remarks>
    [CompilerGeneratedAttribute()]
    class NamespaceDoc
    {
    }
}
