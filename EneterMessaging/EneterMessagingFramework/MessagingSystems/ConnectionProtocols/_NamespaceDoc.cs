/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/

using System.Runtime.CompilerServices;


namespace Eneter.Messaging.MessagingSystems.ConnectionProtocols
{
    /// <summary>
    /// Encoding/decoding the communication between output and input channels.
    /// </summary>
    /// <remarks>
    /// The protocol formatter encodes low-level messages sent between output and input channels.
    /// The output channel can send following messages to the input channel:
    /// <ul>
    /// <li><b>Open Connection</b> - Output channel sends this message to the input channel when it opens the connection.</li>
    /// <li><b>Close Connection</b> - Output channel sends this message to the input channel when it closes the connection.</li>
    /// <li><b>Message</b> - output channel uses this message when it sends a data message to the input channel.</li>
    /// </ul>
    /// The input channel can send following messages to the input channel:
    /// <ul>
    /// <li><b>Close Connection</b> - The input channel sends this message to the output channel when it disconnects the output channel. </li>
    /// <li><b>Message</b> - The input channel sends this message when it sends a data message to the output channel.</li>
    /// </ul>
    /// </remarks>
    [CompilerGeneratedAttribute()]
    class NamespaceDoc
    {
    }
}
