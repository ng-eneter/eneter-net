/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using Eneter.Messaging.Infrastructure.Attachable;

namespace Eneter.Messaging.Nodes.Dispatcher
{
    /// <summary>
    /// Declares the dispatcher.
    /// </summary>
    /// <remarks>
    /// The dispatcher has attached more input channels and more output channels.<br/>
    /// When it receives some message via the input channel, it forwards the message to all output channels.<br/>
    /// This is the one-way dispatcher. It means it can forward messages but cannot route back response messages.<br/>
    /// </remarks>
    public interface IDispatcher : IAttachableMultipleOutputChannels, IAttachableMultipleInputChannels
    {
    }
}
