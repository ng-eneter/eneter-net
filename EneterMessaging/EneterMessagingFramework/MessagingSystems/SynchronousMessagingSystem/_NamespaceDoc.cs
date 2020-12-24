

using System.Runtime.CompilerServices;

namespace Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem
{
    /// <summary>
    /// Synchronous communication within one process (like a synchronous local call).
    /// </summary>
    /// <remarks>
    /// This messaging system transfers messages synchronously in the context of the calling thread.
    /// Therefore the calling thread is blocked until the message is delivered and processed.
    /// The messaging system is very fast and is suitable to deliver messages between components
    /// within one process.
    /// </remarks>
    [CompilerGeneratedAttribute()]
    class NamespaceDoc
    {
    }
}
