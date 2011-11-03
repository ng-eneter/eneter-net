/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using Eneter.Messaging.DataProcessing.Sequencing;

namespace Eneter.Messaging.EndPoints.Commands
{
    /// <summary>
    /// The interface represents the context of the particular command execution.
    /// It is passed as the input parameter to the method performing the command.
    /// </summary>
    /// <typeparam name="_ReturnDataType">type of return data</typeparam>
    /// <typeparam name="_InputDataType">type of input data</typeparam>
    public interface ICommandContext<_ReturnDataType, _InputDataType>
    {
        /// <summary>
        /// Returns identifier of the command proxy that executed this command.
        /// </summary>
        string CommandProxyId { get; }

        /// <summary>
        /// Returns identifier of this command. (If one command proxy executes more commands they can be recognized with this id.)
        /// </summary>
        string CommandId { get; }

        /// <summary>
        /// Returns the input data for the command.
        /// The command proxy has a possibility to send the input data as a sequence. Therefore the input data coming in
        /// fragments are put to the queue, from where they can be removed by this method.
        /// If the queue is empty the calling thread is blocked until the input data fragment is received.<br/>
        /// Note: The parameter index in DataFragment is not used and is set to -1.
        /// </summary>
        /// <returns>data fragment wrapping the input data</returns>
        DataFragment<_InputDataType> DequeueInputData();

        /// <summary>
        /// Returns the input data for the command.
        /// The command proxy has a possibility to send the input data as a sequence. Therefore the input data coming in
        /// fragments are put to the queue, from where they can be removed by this method.
        /// If the queue is empty the calling thread is blocked until
        /// the input data fragment is received or the specified timeout occured.
        /// Note: The parameter index in DataFragment is not used and is set to -1.
        /// </summary>
        /// <param name="millisecondsTimeout">maximum waiting time for the input data. If the time is exceeded the TimeoutException is thrown.</param>
        /// <returns>data fragment wrapping the input data</returns>
        /// <exception cref="TimeoutException">when the maximum waiting time for the input data is exceeded</exception>
        DataFragment<_InputDataType> DequeueInputData(int millisecondsTimeout);

        /// <summary>
        /// Returns number of input data in the queue.
        /// </summary>
        int NumberOfInputData { get; }

        /// <summary>
        /// The currently set request for the command e.g. Pause, Resume or Cancel.
        /// </summary>
        ECommandRequest CurrentRequest { get; }

        /// <summary>
        /// Returns true if the command proxy that executed the command is still connected.
        /// </summary>
        bool IsCommandProxyConnected { get; }

        /// <summary>
        /// If the pause is the current request then it blocks the calling thread until resumed or canceled.
        /// </summary>
        void WaitIfPause();

        /// <summary>
        /// If the pause is requested then it blocks the calling thread until resumed or canceled.
        /// </summary>
        /// <param name="millisecondsTimeout">maximum waiting time in miliseconds</param>
        /// <returns>true if the timeout did not occur</returns>
        bool WaitIfPause(int millisecondsTimeout);

        /// <summary>
        /// Notifies the command proxy that the command was paused.
        /// </summary>
        void ResponsePause();

        /// <summary>
        /// Notifies the command proxy that the command was canceled.
        /// </summary>
        void ResponseCancel();

        /// <summary>
        /// Notifies the command proxy that the command failed.
        /// </summary>
        /// <param name="errorMessage">error message</param>
        void ResponseFailure(string errorMessage);

        /// <summary>
        /// Sends the return data to the command proxy.
        /// </summary>
        /// <param name="commandState">command state</param>
        /// <param name="returnData">return data</param>
        void Response(ECommandState commandState, _ReturnDataType returnData);

        /// <summary>
        /// Sends the return data to the command proxy.
        /// The method can be used if the return data is sent in more fragments.
        /// E.g. If the command wants to notify progress or some partial results.
        /// </summary>
        /// <param name="commandState">command state</param>
        /// <param name="returnData">return data</param>
        /// <param name="sequenceId">identifies the sequence where the fragment of return data belongs</param>
        /// <param name="isReturnDataSequenceCompleted">true if this is the last fragment of the sequence</param>
        void Response(ECommandState commandState, _ReturnDataType returnData, string sequenceId, bool isReturnDataSequenceCompleted);
    }
}
