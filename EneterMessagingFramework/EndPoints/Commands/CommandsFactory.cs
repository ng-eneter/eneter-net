/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.EndPoints.TypedSequencedMessages;

namespace Eneter.Messaging.EndPoints.Commands
{
    /// <summary>
    /// Implements the factory to create command proxy and command.
    /// </summary>
    public class CommandsFactory : ICommandsFactory
    {
        /// <summary>
        /// Constructs the factory with binary serializer.
        /// <b>Note: The serializer is XmlStringSerializer in case of Silverlight.</b>
        /// </summary>
        public CommandsFactory()
        {
            using (EneterTrace.Entering())
            {
                myDuplexTypedSequencedMessagesFactory = new DuplexTypedSequencedMessagesFactory();
            }
        }

        /// <summary>
        /// Constructs the factory with specified serializer.
        /// </summary>
        /// <param name="serializer">serializer</param>
        public CommandsFactory(ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                myDuplexTypedSequencedMessagesFactory = new DuplexTypedSequencedMessagesFactory(serializer);
            }
        }

        /// <summary>
        /// Constructs the factory with the specified duplex typed sequenced messages factory.
        /// The duplex typed sequenced messages are used internally by the command (and command proxy) so
        /// you can provide your own implementation if needed.
        /// </summary>
        /// <param name="duplexTypedSequencedMessagesFactory">duplex typed sequenced messages factory</param>
        public CommandsFactory(IDuplexTypedSequencedMessagesFactory duplexTypedSequencedMessagesFactory)
        {
            using (EneterTrace.Entering())
            {
                myDuplexTypedSequencedMessagesFactory = duplexTypedSequencedMessagesFactory;
            }
        }


        /// <summary>
        /// Creates the command proxy.
        /// </summary>
        /// <typeparam name="_ReturnDataType">type of return data</typeparam>
        /// <typeparam name="_InputDataType">type of input data</typeparam>
        /// <returns>command proxy</returns>
        public ICommandProxy<_ReturnDataType, _InputDataType> CreateCommandProxy<_ReturnDataType, _InputDataType>()
        {
            using (EneterTrace.Entering())
            {
                return new CommandProxy<_ReturnDataType, _InputDataType>(myDuplexTypedSequencedMessagesFactory);
            }
        }

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
        public ICommand<_ReturnDataType, _InputDataType> CreateCommand<_ReturnDataType, _InputDataType>(Action<ICommandContext<_ReturnDataType, _InputDataType>> methodProcessingCommand, EProcessingStrategy processingStrategy)
        {
            using (EneterTrace.Entering())
            {
                return new Command<_ReturnDataType, _InputDataType>(methodProcessingCommand, processingStrategy, myDuplexTypedSequencedMessagesFactory);
            }
        }


        private IDuplexTypedSequencedMessagesFactory myDuplexTypedSequencedMessagesFactory;
    }
}
