/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;

namespace Eneter.Messaging.EndPoints.Commands
{
    /// <summary>
    /// Event data when the response from the command was received.
    /// </summary>
    /// <typeparam name="_ReturnDataType">type of return data</typeparam>
    public sealed class CommandResponseReceivedEventArgs<_ReturnDataType> : EventArgs
    {
        /// <summary>
        /// Constructs the event data.
        /// </summary>
        /// <param name="commandId">command identifier</param>
        /// <param name="commandState">state of the command</param>
        /// <param name="returnData">return data coming from the command</param>
        /// <param name="sequenceId">return data sequence id</param>
        /// <param name="isSequenceCompleted">tru - if the sequence of return data is completed</param>
        /// <param name="commandError">error message coming from the command</param>
        public CommandResponseReceivedEventArgs(string commandId, ECommandState commandState, _ReturnDataType returnData, string sequenceId, bool isSequenceCompleted, string commandError)
        {
            CommandState = commandState;
            ReturnData = returnData;
            SequenceId = sequenceId;
            IsSequenceCompleted = isSequenceCompleted;
            CommandError = commandError;
            CommandId = commandId;
        }

        /// <summary>
        /// Constructs the event data when an error was detected during receiving the response.
        /// (e.g. a deserialization error)
        /// </summary>
        /// <param name="receivingError"></param>
        public CommandResponseReceivedEventArgs(Exception receivingError)
        {
            CommandState = ECommandState.NotApplicable;
            ReceivingError = receivingError;
            SequenceId = "";
            CommandError = "";
            CommandId = "";
        }

        /// <summary>
        /// Gets command state.
        /// </summary>
        public ECommandState CommandState { get; private set; }

        /// <summary>
        /// Gets return data from the command.
        /// </summary>
        public _ReturnDataType ReturnData { get; private set; }

        /// <summary>
        /// Gets the sequence is of return data from the command.
        /// </summary>
        public string SequenceId { get; private set; }

        /// <summary>
        /// Returns true - if the sequence of return data is completed.
        /// </summary>
        public bool IsSequenceCompleted { get; private set; }

        /// <summary>
        /// Returns an error detected during receiving the response. (e.g. deserialization error)
        /// </summary>
        public Exception ReceivingError { get; private set; }

        /// <summary>
        /// Returns an error message coming from the command.
        /// </summary>
        public string CommandError { get; private set; }

        /// <summary>
        /// Returns command identifier.
        /// </summary>
        public string CommandId { get; private set; }
    }
}
