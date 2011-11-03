/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using Eneter.Messaging.Infrastructure.Attachable;

namespace Eneter.Messaging.EndPoints.TypedSequencedMessages
{
    /// <summary>
    /// The interface declares the typed message receiver that receives the sequence of messages.
    /// It is guaranteed the received sequence has the same order as was sent.
    /// </summary>
    public interface ITypedSequencedMessageReceiver<_MessageDataType> : IAttachableInputChannel
    {
        /// <summary>
        /// The event is invoked when the typed message (as a fragment of the sequence) is received.
        /// </summary>
        event EventHandler<TypedSequencedMessageReceivedEventArgs<_MessageDataType>> MessageReceived;
    }
}
