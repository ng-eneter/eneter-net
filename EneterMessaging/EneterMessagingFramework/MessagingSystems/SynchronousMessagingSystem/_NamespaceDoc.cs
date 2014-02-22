/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System.Runtime.CompilerServices;

namespace Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem
{
    /// <summary>
    /// Synchronous communication within one process (like a synchronous local call).
    /// </summary>
    /// <remarks>
    /// This messaging system transfers messages synchronously in the context of the calling thread.
    /// Therefore the calling thread is blocked until the message is delivered and processed.
    /// However, the notification events (e.g. connection opened, ...) can come in a different thread.
    /// The messaging system is very fast and is suitable to deliver messages locally between internal communication components.
    /// </remarks>
    [CompilerGeneratedAttribute()]
    class NamespaceDoc
    {
    }
}
