

using System.Runtime.CompilerServices;

namespace Eneter.Messaging.MessagingSystems.MessagingSystemBase
{
    /// <summary>
    /// Messaging system interfaces.
    /// </summary>
    /// <remarks>
    /// The messaging system is responsible for transferring messages via channels.<br/>
    /// The input channel represents the service which is identified by its address.
    /// It can accept connections from multiple output channels and receive/send messages from/to connected output channels.<br/>
    /// The output channel represents the client side. It can connect the input channel (by specifying the address).
    /// Then it can send/receive messages to/from the connected input channel.
    /// </remarks>
    [CompilerGeneratedAttribute()]
    class NamespaceDoc
    {
    }
}
