/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using Eneter.Messaging.Infrastructure.Attachable;

namespace Eneter.Messaging.Nodes.ChannelWrapper
{
    /// <summary>
    /// Declares the channel unwrapper.
    /// </summary>
    /// <remarks>
    /// The channel wrapper is listening to more input channels. When it receives some message,
    /// it wraps the message and sends it via the only output channel.
    /// On the other side the message is received by channel unwrapper. The unwrapper unwraps the message
    /// and uses the output channel to forward the message to the correct receiver.<br/>
    /// The message can be sent only one-way. For the bidirectional communication see <see cref="IDuplexChannelWrapper"/> and
    /// <see cref="IDuplexChannelUnwrapper"/>.<br/>
    /// Notice, the 'channel unwrapper' can communication only with 'channel wrapper'. It cannot communicate with 'duplex channel wrapper'.
    /// </remarks>
    public interface IChannelUnwrapper : IAttachableInputChannel
    {
    }
}
