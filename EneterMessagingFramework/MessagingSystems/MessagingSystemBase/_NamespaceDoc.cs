/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System.Runtime.CompilerServices;

namespace Eneter.Messaging.MessagingSystems.MessagingSystemBase
{
    /// <summary>
    /// Interfaces representing the messaging system.
    /// </summary>
    /// <remarks>
    /// The messaging system is responsible for delivering messages from a sender to a receiver through communication channels.
    /// <br/><br/>
    /// For the one-way communication, the messaging system provides the output channel and the input channel.
    /// The output channel sends messages to the input channel with the same channel id.
    /// <br/><br/>
    /// For the bidirectional communication, the messaging system provides the duplex output channel and the duplex input channel.
    /// The duplex output channel sends messages to the duplex input channel with the same channel id and can receive response messages.
    /// The duplex input channel receives messages and can send back response messages.
    /// </remarks>
    [CompilerGeneratedAttribute()]
    class NamespaceDoc
    {
    }
}
