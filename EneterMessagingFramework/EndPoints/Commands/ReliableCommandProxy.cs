/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.EndPoints.TypedSequencedMessages;
using Eneter.Messaging.MessagingSystems.Composites.ReliableMessagingComposit;

namespace Eneter.Messaging.EndPoints.Commands
{
    internal class ReliableCommandProxy<_ReturnDataType, _InputDataType> : IReliableCommandProxy<_ReturnDataType, _InputDataType>
    {
        public event EventHandler<CommandResponseReceivedEventArgs<_ReturnDataType>> CommandResponseReceived;

        public event EventHandler<MessageIdEventArgs> MessageDelivered;

        public event EventHandler<MessageIdEventArgs> MessageNotDelivered;


        public ReliableCommandProxy(IReliableTypedSequencedMessagesFactory reliableTypedSequencedMessagesFactory)
        {
            using (EneterTrace.Entering())
            {
                myMessageSender = reliableTypedSequencedMessagesFactory.CreateReliableTypedSequencedMessageSender<CommandReturnData<_ReturnDataType>, CommandInputData<_InputDataType>>();
                myMessageSender.ResponseReceived += OnResponseReceived;
                myMessageSender.MessageDelivered += OnMessageDelivered;
                myMessageSender.MessageNotDelivered += OnMessageNotDelivered;
            }
        }

        public void AttachReliableOutputChannel(IReliableDuplexOutputChannel reliableDuplexOutputChannel)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    myMessageSender.AttachReliableOutputChannel(reliableDuplexOutputChannel);
                    myDuplexOutputChannelId = reliableDuplexOutputChannel.ChannelId;
                }
                catch (Exception err)
                {
                    string aChannelId = (reliableDuplexOutputChannel != null) ? reliableDuplexOutputChannel.ChannelId : string.Empty;
                    EneterTrace.Error(TracedObject + "failed to attach duplex output channel '" + aChannelId + "' and open connection.", err);
                    throw;
                }
            }
        }


        public void DetachReliableOutputChannel()
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    myMessageSender.DetachReliableOutputChannel();
                    myDuplexOutputChannelId = string.Empty;
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + "failed to correctly detach the duplex output channel '" + myDuplexOutputChannelId + "'.", err);
                }
            }
        }

        public bool IsReliableOutputChannelAttached
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    return myMessageSender.IsReliableOutputChannelAttached;
                }
            }
        }

        public IReliableDuplexOutputChannel AttachedReliableOutputChannel
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    return myMessageSender.AttachedReliableOutputChannel;
                }
            }
        }

        public string Pause(string commandId)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    CommandInputData<_InputDataType> anInputData = new CommandInputData<_InputDataType>(commandId, ECommandRequest.Pause, default(_InputDataType));
                    return myMessageSender.SendMessage(anInputData, "NA", true);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to send pause request.", err);
                    throw;
                }
            }
        }


        public string Resume(string commandId)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    CommandInputData<_InputDataType> anInputData = new CommandInputData<_InputDataType>(commandId, ECommandRequest.Resume, default(_InputDataType));
                    return myMessageSender.SendMessage(anInputData, "NA", true);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to send resume request.", err);
                    throw;
                }
            }
        }

        public string Cancel(string commandId)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    CommandInputData<_InputDataType> anInputData = new CommandInputData<_InputDataType>(commandId, ECommandRequest.Cancel, default(_InputDataType));
                    return myMessageSender.SendMessage(anInputData, "NA", true);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to send cancel request.", err);
                    throw;
                }
            }
        }

        public string Execute(string commandId)
        {
            using (EneterTrace.Entering())
            {
                return Execute(commandId, default(_InputDataType), "NA", true);
            }
        }

        public string Execute(string commandId, _InputDataType inputData)
        {
            using (EneterTrace.Entering())
            {
                return Execute(commandId, inputData, "NA", true);
            }
        }

        public string Execute(string commandId, _InputDataType inputData, string sequenceId, bool isSequenceCompleted)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    CommandInputData<_InputDataType> anInputData = new CommandInputData<_InputDataType>(commandId, ECommandRequest.Execute, inputData);
                    return myMessageSender.SendMessage(anInputData, sequenceId, isSequenceCompleted);
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
                        EneterTrace.Error(TracedObject + "detected an error during receiving of the response.", e.ReceivingError);

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

        private void OnMessageDelivered(object sender, MessageIdEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (MessageDelivered != null)
                {
                    try
                    {
                        MessageDelivered(this, e);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }

        private void OnMessageNotDelivered(object sender, MessageIdEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (MessageNotDelivered != null)
                {
                    try
                    {
                        MessageNotDelivered(this, e);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }


        private IReliableTypedSequencedMessageSender<CommandReturnData<_ReturnDataType>, CommandInputData<_InputDataType>> myMessageSender;

        /// <summary>
        /// This is used only for trace purposes.
        /// </summary>
        private string myDuplexOutputChannelId = "";
        private string TracedObject
        {
            get
            {
                return "The ReliableCommandProxy<" + typeof(_ReturnDataType).Name + ", " + typeof(_InputDataType).Name + " > atached to the duplex output channel '" + myDuplexOutputChannelId + "' ";
            }
        }
    }
}
