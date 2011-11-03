/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using System.Threading;
using Eneter.Messaging.DataProcessing.MessageQueueing;
using Eneter.Messaging.DataProcessing.Sequencing;
using Eneter.Messaging.EndPoints.TypedSequencedMessages;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.EndPoints.Commands
{
    internal class CommandItem<_ReturnDataType, _InputDataType> : ICommandContext<_ReturnDataType, _InputDataType>
    {
        public CommandItem(string commandProxyId, string commandId, IDuplexTypedSequencedMessageReceiver<CommandReturnData<_ReturnDataType>, CommandInputData<_InputDataType>> messageReceiver)
        {
            using (EneterTrace.Entering())
            {
                CommandProxyId = commandProxyId;
                CommandId = commandId;

                myMessageReceiver = messageReceiver;

                myCurrentRequest = ECommandRequest.Execute;
            }
        }


        public void EnqueueInputData(_InputDataType inputData, string sequenceId, bool isSequenceCompleted)
        {
            using (EneterTrace.Entering())
            {
                DataFragment<_InputDataType> aDataFragment = new DataFragment<_InputDataType>(inputData, sequenceId, -1, isSequenceCompleted);

                myInputDataQueue.EnqueueMessage(aDataFragment);
            }
        }

        public void Pause()
        {
            using (EneterTrace.Entering())
            {
                lock (myCurrentRequestLock)
                {
                    myCurrentRequest = ECommandRequest.Pause;

                    // Block the thread performing the command.
                    myContinueProcessingEvent.Reset();
                }
            }
        }

        public void Resume()
        {
            using (EneterTrace.Entering())
            {
                lock (myCurrentRequestLock)
                {
                    myCurrentRequest = ECommandRequest.Resume;

                    // Release the thread performing the command.
                    myContinueProcessingEvent.Set();
                }
            }
        }

        public void Cancel()
        {
            using (EneterTrace.Entering())
            {
                lock (myCurrentRequestLock)
                {
                    myCurrentRequest = ECommandRequest.Cancel;

                    // If the thread performing the command is blocked by pause, release it.
                    myContinueProcessingEvent.Set();

                    // If the thread performing the command is blocked by waiting for input data, release it.
                    myInputDataQueue.UnblockProcessingThreads();
                }
            }
        }


        public DataFragment<_InputDataType> DequeueInputData()
        {
            using (EneterTrace.Entering())
            {
                return myInputDataQueue.DequeueMessage() as DataFragment<_InputDataType>;
            }
        }

        public DataFragment<_InputDataType> DequeueInputData(int millisecondsTimeout)
        {
            using (EneterTrace.Entering())
            {
                return myInputDataQueue.DequeueMessage(millisecondsTimeout) as DataFragment<_InputDataType>;
            }
        }

        public int NumberOfInputData { get { return myInputDataQueue.Count; } }

        public ECommandRequest CurrentRequest
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    lock (myCurrentRequestLock)
                    {
                        return myCurrentRequest;
                    }
                }
            }
        }

        public bool IsCommandProxyConnected { get; set; }


        public void WaitIfPause()
        {
            using (EneterTrace.Entering())
            {
                myContinueProcessingEvent.WaitOne();
            }
        }

        public bool WaitIfPause(int millisecondsTimeout)
        {
            using (EneterTrace.Entering())
            {
                return myContinueProcessingEvent.WaitOne(millisecondsTimeout);
            }
        }

        public void ResponsePause()
        {
            using (EneterTrace.Entering())
            {
                Response(ECommandState.Paused, default(_ReturnDataType), "NA", true, "");
            }
        }

        public void ResponseCancel()
        {
            using (EneterTrace.Entering())
            {
                Response(ECommandState.Canceled, default(_ReturnDataType), "NA", true, "");
            }
        }

        public void ResponseFailure(string errorMessage)
        {
            using (EneterTrace.Entering())
            {
                Response(ECommandState.Failed, default(_ReturnDataType), "NA", true, errorMessage);
            }
        }

        public void Response(ECommandState commandState, _ReturnDataType returnData)
        {
            using (EneterTrace.Entering())
            {
                Response(commandState, returnData, "NA", true, "");
            }
        }

        public void Response(ECommandState commandState, _ReturnDataType returnData, string sequenceId, bool isReturnDataSequenceCompleted)
        {
            using (EneterTrace.Entering())
            {
                Response(commandState, returnData, sequenceId, isReturnDataSequenceCompleted, "");
            }
        }


        private void Response(ECommandState commandState, _ReturnDataType returnData, string sequenceId, bool isReturnDataSequenceCompleted, string errorMessage)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    CommandReturnData<_ReturnDataType> aReturnData = new CommandReturnData<_ReturnDataType>(CommandId, commandState, returnData, errorMessage);
                    myMessageReceiver.SendResponseMessage(CommandProxyId, aReturnData, sequenceId, isReturnDataSequenceCompleted);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to send the response.", err);
                    throw;
                }
            }
        }


        public string CommandProxyId { get; private set; }
        public string CommandId { get; private set; }


        private object myCurrentRequestLock = new object();
        private ECommandRequest myCurrentRequest;

        private MessageQueue<object> myInputDataQueue = new MessageQueue<object>();
        private ManualResetEvent myContinueProcessingEvent = new ManualResetEvent(true);

        private IDuplexTypedSequencedMessageReceiver<CommandReturnData<_ReturnDataType>, CommandInputData<_InputDataType>> myMessageReceiver;

        private string TracedObject
        {
            get
            {
                return "The CommandContext<" + typeof(_ReturnDataType).Name + ", " + typeof(_InputDataType).Name + " > with commandId '" + CommandId + "' ";
            }
        }
    }
}
