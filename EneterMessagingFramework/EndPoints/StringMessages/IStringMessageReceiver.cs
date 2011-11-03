/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using Eneter.Messaging.Infrastructure.Attachable;

namespace Eneter.Messaging.EndPoints.StringMessages
{
    /// <summary>
    /// The interface declares the string message receiver.
    /// The receiver is able to receive text messages via one-way input channel.
    /// </summary>
    public interface IStringMessageReceiver : IAttachableInputChannel
    {
        /// <summary>
        /// The event is invoked when a string message was received.
        /// </summary>
        event EventHandler<StringMessageEventArgs> MessageReceived;
    }
}
