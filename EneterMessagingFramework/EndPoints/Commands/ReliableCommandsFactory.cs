/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.EndPoints.TypedSequencedMessages;

namespace Eneter.Messaging.EndPoints.Commands
{
    /// <summary>
    /// Implements the factory to create reliable command proxy and reliable command.
    /// The reliable 'command proxy' and 'command' send acknowledge messages as a confirmation that the request
    /// or the resposne was delivered.<br/>
    /// The reliable command proxy can be used only with reliable command.
    /// </summary>
    public class ReliableCommandsFactory : IReliableCommandsFactory
    {
        /// <summary>
        /// Constructs the factory with binary serializer.<br/>
        /// <b>Note: The serializer is XmlStringSerializer in case of Silverlight.</b>
        /// </summary>
        public ReliableCommandsFactory()
        {
            using (EneterTrace.Entering())
            {
                myReliableTypedSequencedMessagesFactory = new ReliableTypedSequencedMessagesFactory();
            }
        }

        /// <summary>
        /// Constructs the factory with specified serializer.
        /// </summary>
        /// <param name="serializer">serializer</param>
        public ReliableCommandsFactory(ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                myReliableTypedSequencedMessagesFactory = new ReliableTypedSequencedMessagesFactory(serializer);
            }
        }

        /// <summary>
        /// Constructs the factory with the specified duplex typed sequenced messages factory.
        /// The duplex typed sequenced messages are used internally by the command (and command proxy) so
        /// you can provide your own implementation if needed.
        /// </summary>
        /// <param name="reliableTypedSequencedMessagesFactory">duplex typed sequenced messages factory</param>
        public ReliableCommandsFactory(IReliableTypedSequencedMessagesFactory reliableTypedSequencedMessagesFactory)
        {
            using (EneterTrace.Entering())
            {
                myReliableTypedSequencedMessagesFactory = reliableTypedSequencedMessagesFactory;
            }
        }

        /// <summary>
        /// Creates the reliable command proxy.
        /// </summary>
        /// <typeparam name="_ReturnDataType">type of return data</typeparam>
        /// <typeparam name="_InputDataType">type of input data</typeparam>
        /// <returns>reliable command proxy</returns>
        public IReliableCommandProxy<_ReturnDataType, _InputDataType> CreateReliableCommandProxy<_ReturnDataType, _InputDataType>()
        {
            using (EneterTrace.Entering())
            {
                return new ReliableCommandProxy<_ReturnDataType, _InputDataType>(myReliableTypedSequencedMessagesFactory);
            }
        }

        /// <summary>
        /// Creates the reliable command.
        /// </summary>
        /// <typeparam name="_ReturnDataType">type of return data</typeparam>
        /// <typeparam name="_InputDataType">type of input data</typeparam>
        /// <returns>reliable command</returns>
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
        public IReliableCommand<_ReturnDataType, _InputDataType> CreateReliableCommand<_ReturnDataType, _InputDataType>(Action<IReliableCommandContext<_ReturnDataType, _InputDataType>> methodProcessingCommand, EProcessingStrategy processingStrategy)
        {
            using (EneterTrace.Entering())
            {
                return new ReliableCommand<_ReturnDataType, _InputDataType>(methodProcessingCommand, processingStrategy, myReliableTypedSequencedMessagesFactory);
            }
        }


        private IReliableTypedSequencedMessagesFactory myReliableTypedSequencedMessagesFactory;
    }
}
