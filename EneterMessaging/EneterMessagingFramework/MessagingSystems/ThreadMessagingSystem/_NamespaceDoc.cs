﻿

using System.Runtime.CompilerServices;

namespace Eneter.Messaging.MessagingSystems.ThreadMessagingSystem
{
    /// <summary>
    /// Communication routing messages into one working thread.
    /// </summary>
    /// <remarks>
    /// The messaging system transferring messages to a working thread.
    /// Received messages are stored in the queue which is then processed by one working thread.
    /// Therefore the messages are processed synchronously but it does not block receiving. 
    /// </remarks>
    [CompilerGeneratedAttribute()]
    class NamespaceDoc
    {
    }
}
