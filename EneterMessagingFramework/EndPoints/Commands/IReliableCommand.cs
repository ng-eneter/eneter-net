/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.Composites.ReliableMessagingComposit;

namespace Eneter.Messaging.EndPoints.Commands
{
    /// <summary>
    /// The interface declares the reliable command.
    /// The command is able to receive typed request messages and return typed response messages.
    /// The command can be paused, resumed or canceled.
    /// In additition the reliable command provides notifications whether the response messages were
    /// received by the reliable proxy command. <br/>
    /// The reliable command can be used only with reliable proxy command.
    /// </summary>
    /// <typeparam name="_ReturnDataType">Type of return data.</typeparam>
    /// <typeparam name="_InputDataType">Type of input data.</typeparam>
    public interface IReliableCommand<_ReturnDataType, _InputDataType> : IAttachableReliableInputChannel
    {
        /// <summary>
        /// The event is invoked when an error was detected during receiving the request. (e.g. a deserialization error)
        /// </summary>
        event EventHandler<CommandReceivingErrorEventArgs> ErrorReceived;

        /// <summary>
        /// The event is invoked when the command proxy was connected to the command.
        /// </summary>
        event EventHandler<CommandRequestEventArgs> CommandProxyConnected;

        /// <summary>
        /// The event is invoked when the command proxy disconnected from the command.
        /// </summary>
        event EventHandler<CommandRequestEventArgs> CommandProxyDisconnected;

        /// <summary>
        /// The event is invoked when the command proxy confirmed the response message was received.
        /// </summary>
        event EventHandler<MessageIdEventArgs> ResponseMessageDelivered;

        /// <summary>
        /// The event is invoked when the command proxy does not confirm, that the response message was delivered.
        /// (If the message is not delivered within desired time.)
        /// </summary>
        event EventHandler<MessageIdEventArgs> ResponseMessageNotDelivered;
    }
}
