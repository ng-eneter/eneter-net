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
    /// The interface declares the factory to create command proxy and command.
    /// </summary>
    public interface ICommandsFactory
    {
        /// <summary>
        /// Creates the command proxy.
        /// </summary>
        /// <typeparam name="_ReturnDataType">type of return data</typeparam>
        /// <typeparam name="_InputDataType">type of input data</typeparam>
        /// <returns>command proxy</returns>
        ICommandProxy<_ReturnDataType, _InputDataType> CreateCommandProxy<_ReturnDataType, _InputDataType>();

        /// <summary>
        /// Creates the command.
        /// </summary>
        /// <typeparam name="_ReturnDataType">type of return data</typeparam>
        /// <typeparam name="_InputDataType">type of input data</typeparam>
        /// <returns>command</returns>
        /// <example>
        /// The following example shows how to implement the method performing a command,
        /// so that the method processes Pause, Resume, Cancel and Failures.
        /// <code>
        /// private void CommandProcessingMethod(ICommandContext&lt;int, bool&gt; commandContext)
        /// {
        ///     try
        ///     {
        ///         // Read the input data.
        ///         DataFragment&lt;bool&gt; anInputDataFragment = commandContext.DequeueInputData();
        ///         bool anInputData = anInputDataFragment.Data;    
        ///
        ///         for (int i = 0; i &lt; 100; ++i)
        ///         {
        ///             // Simulate some work.
        ///             Thread.Sleep(100);
        /// 
        ///             // Wait if pause is requested.
        ///             if (commandContext.CurrentRequest == ECommandRequest.Pause)
        ///             {
        ///                 // Notify the command proxy that the command was paused.
        ///                 commandContext.ResponsePause();
        /// 
        ///                 // Wait until resumed or canceled.
        ///                 commandContext.WaitIfPause();
        ///             }
        /// 
        ///             // If the cancel is requested then stop the command.
        ///             // Note: The check for the cancel is placed after the check for the pause
        ///             //       because when the command is paused it can be canceled.
        ///             if (commandContext.CurrentRequest == ECommandRequest.Cancel)
        ///             {
        ///                 // Notify the command proxy that the command was canceled.
        ///                 commandContext.ResponseCancel();
        ///                 return;
        ///             }
        /// 
        ///             // Notify the progress.
        ///             ECommandState aState = (i == 99) ? ECommandState.Completed : ECommandState.InProgress;
        ///             commandContext.Response(aState, i + 1);
        ///         }
        ///     }
        ///     catch (Exception err)
        ///     {
        ///         commandContext.ResponseFailure(err.Message);
        ///     }
        /// }
        /// </code>
        /// </example>
        ICommand<_ReturnDataType, _InputDataType> CreateCommand<_ReturnDataType, _InputDataType>(Action<ICommandContext<_ReturnDataType, _InputDataType>> methodProcessingCommand, EProcessingStrategy processingStrategy);
    }
}
