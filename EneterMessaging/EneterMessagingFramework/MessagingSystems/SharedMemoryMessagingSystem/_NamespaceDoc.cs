

#if !NET35 && NETFRAMEWORK

using System.Runtime.CompilerServices;

namespace Eneter.Messaging.MessagingSystems.SharedMemoryMessagingSystem
{
    /// <summary>
    /// Communication via shared memory. (Faster than Named Pipes.)
    /// </summary>
    /// <remarks>
    /// It transfers messages between processes running on the same machine using shared memory.
    /// Transferring messages via the shared memory is faster than using Named Pipes.<br/>
    /// Messaging via the shared memeory is supported only in .Net 4.0 or higher.
    /// </remarks>
    [CompilerGeneratedAttribute()]
    class NamespaceDoc
    {
    }
}

#endif