/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System.Runtime.CompilerServices;

namespace Eneter.Messaging.MessagingSystems.Composites.BufferedMessagingComposit
{
    /// <summary>
    /// Buffering of messages in case the network connection is not available.
    /// </summary>
    /// <remarks>
    /// The buffered messaging is intended to temporarily store sent messages while the network connection is not available.
    /// Typical scenarios are:
    /// <br/><br/>
    /// <b>Short disconnections</b><br/>
    /// The network connection is unstable and can be anytime interrupted. In case of the disconnection, the sent messages are stored
    /// in the memory buffer while the connection tries to be automatically reopen. If the reopen is successful and the connection
    /// is established, the messages are sent from the buffer.
    /// <br/><br/>
    /// <b>Independent startup order</b><br/>
    /// The communicating applications starts in undefined order and initiates the communication. In case the application receiving
    /// messages is not up, the sent messages are stored in the buffer. Then when the receiving application is running, the messages
    /// are automatically sent from the buffer.
    /// </remarks>
    [CompilerGeneratedAttribute()]
    class NamespaceDoc
    {
    }
}
