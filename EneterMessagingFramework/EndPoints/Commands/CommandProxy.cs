/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.EndPoints.TypedSequencedMessages;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.EndPoints.Commands
{
    internal class CommandProxy<_ReturnDataType, _InputDataType> : ICommandProxy<_ReturnDataType, _InputDataType>
    {
        public event EventHandler<CommandResponseReceivedEventArgs<_ReturnDataType>> CommandResponseReceived;


        public CommandProxy(IDuplexTypedSequencedMessagesFactory duplexTypedSequencedMessagesFactory)
        {
            using (EneterTrace.Entering())
            {
                myMessageSender = duplexTypedSequencedMessagesFactory.CreateDuplexTypedSequencedMessageSender<CommandReturnData<_ReturnDataType>, CommandInputData<_InputDataType>>();
                myMessageSender.ResponseReceived += OnResponseReceived;
            }
        }

        public void AttachDuplexOutputChannel(IDuplexOutputChannel duplexOutputChannel)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    myMessageSender.AttachDuplexOutputChannel(duplexOutputChannel);
                    myDuplexOutputChannelId = duplexOutputChannel.ChannelId;
                }
                catch (Exception err)
                {
                    string aChannelId = (duplexOutputChannel != null) ? duplexOutputChannel.ChannelId : string.Empty;
                    EneterTrace.Error(TracedObject + "failed to attach duplex output channel '" + aChannelId + "' and open connection.", err);
                    throw;
                }
            }
        }


        public void DetachDuplexOutputChannel()
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    myMessageSender.DetachDuplexOutputChannel();
                    myDuplexOutputChannelId = string.Empty;
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + "failed to correctly detach the duplex output channel '" + myDuplexOutputChannelId + "'.", err);
                }
            }
        }

        public bool IsDuplexOutputChannelAttached
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    return myMessageSender.IsDuplexOutputChannelAttached;
                }
            }
        }

        public IDuplexOutputChannel AttachedDuplexOutputChannel
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    return myMessageSender.AttachedDuplexOutputChannel;
                }
            }
        }

        public void Pause(string commandId)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    CommandInputData<_InputDataType> anInputData = new CommandInputData<_InputDataType>(commandId, ECommandRequest.Pause, default(_InputDataType));
                    myMessageSender.SendMessage(anInputData, "NA", true);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to send pause request.", err);
                    throw;
                }
            }
        }


        public void Resume(string commandId)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    CommandInputData<_InputDataType> anInputData = new CommandInputData<_InputDataType>(commandId, ECommandRequest.Resume, default(_InputDataType));
                    myMessageSender.SendMessage(anInputData, "NA", true);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to send resume request.", err);
                    throw;
                }
            }
        }

        public void Cancel(string commandId)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    CommandInputData<_InputDataType> anInputData = new CommandInputData<_InputDataType>(commandId, ECommandRequest.Cancel, default(_InputDataType));
                    myMessageSender.SendMessage(anInputData, "NA", true);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to send cancel request.", err);
                    throw;
                }
            }
        }

        public void Execute(string commandId)
        {
            using (EneterTrace.Entering())
            {
                Execute(commandId, default(_InputDataType), "NA", true);
            }
        }

        public void Execute(string commandId, _InputDataType inputData)
        {
            using (EneterTrace.Entering())
            {
                Execute(commandId, inputData, "NA", true);
            }
        }

        public void Execute(string commandId, _InputDataType inputData, string sequenceId, bool isSequenceCompleted)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    CommandInputData<_InputDataType> anInputData = new CommandInputData<_InputDataType>(commandId, ECommandRequest.Execute, inputData);
                    myMessageSender.SendMessage(anInputData, sequenceId, isSequenceCompleted);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to send execute request.", err);
                    throw;
                }
            }
        }


        private void OnResponseReceived(object sender, TypedSequencedResponseReceivedEventArgs<CommandReturnData<_ReturnDataType>> e)
        {
            using (EneterTrace.Entering())
            {
                if (CommandResponseReceived != null)
                {
                    CommandResponseReceivedEventArgs<_ReturnDataType> aResponseEvent = null;

                    if (e.ReceivingError == null)
                    {
                        aResponseEvent = new CommandResponseReceivedEventArgs<_ReturnDataType>(e.ResponseMessage.CommandId, e.ResponseMessage.CommandState, e.ResponseMessage.ReturnData, e.SequenceId, e.IsSequenceCompleted, e.ResponseMessage.ErrorMessage);
                    }
                    else
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, e.ReceivingError);

                        aResponseEvent = new CommandResponseReceivedEventArgs<_ReturnDataType>(e.ReceivingError);
                    }

                    try
                    {
                        CommandResponseReceived(this, aResponseEvent);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }


        private IDuplexTypedSequencedMessageSender<CommandReturnData<_ReturnDataType>, CommandInputData<_InputDataType>> myMessageSender;

        /// <summary>
        /// This is used only for trace purposes.
        /// </summary>
        private string myDuplexOutputChannelId = "";
        private string TracedObject
        {
            get
            {
                return "The CommandProxy<" + typeof(_ReturnDataType).Name + ", " + typeof(_InputDataType).Name + " > atached to the duplex output channel '" + myDuplexOutputChannelId + "' ";
            }
        }
    }
}
