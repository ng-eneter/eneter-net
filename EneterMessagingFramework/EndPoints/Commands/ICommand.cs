/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using Eneter.Messaging.Infrastructure.Attachable;

namespace Eneter.Messaging.EndPoints.Commands
{
    /// <summary>
    /// The interface declares the reliable command.
    /// The command is able to receive typed request messages and return typed response messages.
    /// The command can be paused, resumed or canceled.
    /// </summary>
    /// <typeparam name="_ReturnDataType">Type of return data.</typeparam>
    /// <typeparam name="_InputDataType">Type of input data.</typeparam>
    public interface ICommand<_ReturnDataType, _InputDataType> : IAttachableDuplexInputChannel
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
    }
}
