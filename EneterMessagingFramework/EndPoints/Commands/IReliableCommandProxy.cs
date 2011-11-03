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
    /// The interface declares the reliable command proxy.
    /// The command proxy is able to send the request to execute some activity in the respective command.
    /// The command proxy is also able to request pause, resume or cancel.
    /// It receive response messages from the command.
    /// In addition, the reliable command proxy notifies whether the request message was delivered.
    /// The reliable command proxy can be used only with the reliable command.
    /// </summary>
    /// <typeparam name="_ReturnDataType">Type of return data.</typeparam>
    /// <typeparam name="_InputDataType">Type of input data.</typeparam>
    public interface IReliableCommandProxy<_ReturnDataType, _InputDataType> : IAttachableReliableOutputChannel
    {
        /// <summary>
        /// The event is invoked when the response message from the command was received.
        /// </summary>
        event EventHandler<CommandResponseReceivedEventArgs<_ReturnDataType>> CommandResponseReceived;

        /// <summary>
        /// The event is invoked when the message was delivered.
        /// </summary>
        event EventHandler<MessageIdEventArgs> MessageDelivered;

        /// <summary>
        /// The message is invoked if the message was not delivered until specified time.
        /// </summary>
        event EventHandler<MessageIdEventArgs> MessageNotDelivered;

        /// <summary>
        /// Sends the pause request to the command.
        /// </summary>
        /// <param name="commandId">identifies the command</param>
        string Pause(string commandId);

        /// <summary>
        /// Sends the resume request to the command.
        /// </summary>
        /// <param name="commandId">identifies the command</param>
        string Resume(string commandId);

        /// <summary>
        /// Sends the cancel request to the command.
        /// </summary>
        /// <param name="commandId">identifies the command</param>
        string Cancel(string commandId);

        /// <summary>
        /// Sends the execute request to the command.
        /// </summary>
        /// <param name="commandId">identifies particular command e.g. when the proxy invokes more commands</param>
        string Execute(string commandId);

        /// <summary>
        /// Sends the execute request to the command.
        /// </summary>
        /// <param name="commandId">identifies particular command e.g. when the proxy invokes more commands</param>
        /// <param name="inputData">input data</param>
        string Execute(string commandId, _InputDataType inputData);

        /// <summary>
        /// Sends the execute request to the command.
        /// The input data is sent as a sequence.
        /// </summary>
        /// <param name="commandId">identifies particular command e.g. when the proxy invokes more commands</param>
        /// <param name="inputData">input data</param>
        /// <param name="sequenceId">input data sequence identifier</param>
        /// <param name="isSequenceCompleted">true - if the sequence is completed</param>
        string Execute(string commandId, _InputDataType inputData, string sequenceId, bool isSequenceCompleted);
    }
}
