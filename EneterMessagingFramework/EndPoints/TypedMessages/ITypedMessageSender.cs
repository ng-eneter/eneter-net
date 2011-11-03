/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using Eneter.Messaging.Infrastructure.Attachable;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    /// <summary>
    /// The interface declares the strongly typed messsage sender.
    /// The sender is able to send messages of the specified type via one-way output channel.
    /// </summary>
    public interface ITypedMessageSender<_MessageData> : IAttachableOutputChannel
    {
        /// <summary>
        /// Sends the message of specified type via IOutputChannel.
        /// </summary>
        void SendMessage(_MessageData messageData);
    }
}
