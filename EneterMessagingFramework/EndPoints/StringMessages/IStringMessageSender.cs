/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using Eneter.Messaging.Infrastructure.Attachable;

namespace Eneter.Messaging.EndPoints.StringMessages
{
    /// <summary>
    /// The interface declares the string message sender.
    /// The sender is able to send text messages via one-way output channel.
    /// </summary>
    public interface IStringMessageSender : IAttachableOutputChannel
    {
        /// <summary>
        /// Sends the string message via the attached output channel.
        /// </summary>
        void SendMessage(string message);
    }
}
