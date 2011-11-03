/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System.Runtime.CompilerServices;

namespace Eneter.Messaging.Nodes.ChannelWrapper
{
    /// <summary>
    /// Functionality for sending/receiving more message types via one channel.
    /// </summary>
    /// <remarks>
    /// The channel wrapper and unwrapper are components allowing to send/receive more messages via one channel.
    /// The user code then does not need to implement if ... then code recognizing particular messages.
    /// It also can help to save channel resources. E.g. application can open only limited number of Http requests.
    /// The channel wrapper has attached more input channels and one output channel.
    /// The symmetric component the channel unwrapper has attached one input channel and more output channels.
    /// </remarks>
    [CompilerGeneratedAttribute()]
    class NamespaceDoc
    {
    }
}
