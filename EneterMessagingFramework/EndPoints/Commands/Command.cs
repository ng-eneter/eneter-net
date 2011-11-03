﻿/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using System.Linq;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.EndPoints.TypedSequencedMessages;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using System.Collections.Generic;
using Eneter.Messaging.DataProcessing.MessageQueueing;

namespace Eneter.Messaging.EndPoints.Commands
{
    internal class Command<_ReturnDataType, _InputDataType> : ICommand<_ReturnDataType, _InputDataType>
    {
        public event EventHandler<CommandReceivingErrorEventArgs> ErrorReceived;
        public event EventHandler<CommandRequestEventArgs> CommandProxyConnected;
        public event EventHandler<CommandRequestEventArgs> CommandProxyDisconnected;


        public Command(Action<ICommandContext<_ReturnDataType, _InputDataType>> commandProcessingMethod, EProcessingStrategy processingStrategy, IDuplexTypedSequencedMessagesFactory duplexTypedSequencedMessagesFactory)
        {
            using (EneterTrace.Entering())
            {
                if (commandProcessingMethod == null)
                {
                    string aMessage = TracedObject + "detected that input parameter 'commandProcessingMethod' is null.";
                    throw new ArgumentException(aMessage);
                }

                myMessageReceiver = duplexTypedSequencedMessagesFactory.CreateDuplexTypedSequencedMessageReceiver<CommandReturnData<_ReturnDataType>, CommandInputData<_InputDataType>>();
                myMessageReceiver.MessageReceived += OnMessageReceived;
                myMessageReceiver.ResponseReceiverConnected += OnCommandProxyConnected;
                myMessageReceiver.ResponseReceiverDisconnected += OnCommandProxyDisconnected;

                // If single thread is required then we need to execute commands in the context of the working thread.
                if (processingStrategy == EProcessingStrategy.SingleThread)
                {
                    myWorkingThread = new WorkingThread<CommandItem<_ReturnDataType, _InputDataType>>("CommandProcessingThread");
                }

                myCommandProcessingDelegate = commandProcessingMethod;
            }
        }

        /// <summary>
        /// Attaches the duplex input channel and starts listening.
        /// </summary>
        /// <param name="duplexInputChannel"></param>
        public void AttachDuplexInputChannel(IDuplexInputChannel duplexInputChannel)
        {
            using (EneterTrace.Entering())
            {
                PerformAttach(myMessageReceiver.AttachDuplexInputChannel, duplexInputChannel);
            }
        }

        /// <summary>
        /// Detaches the duplex input channel and stops the listening.
        /// </summary>
        public void DetachDuplexInputChannel()
        {
            using (EneterTrace.Entering())
            {
                lock (myDuplexInputChannelManipulatorLock)
                {
                    try
                    {
                        myMessageReceiver.DetachDuplexInputChannel();
                        myDuplexInputChannelId = "";
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + "failed to correctly detach the duplex input channel.", err);
                    }

                    if (myWorkingThread != null)
                    {
                        try
                        {
                            myWorkingThread.UnregisterMessageHandler();
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + "failed to unregister the working thread handler executing commands.", err);
                        }
                    }
                }
            }
        }

        public bool IsDuplexInputChannelAttached { get { return myMessageReceiver.IsDuplexInputChannelAttached; } }

        public IDuplexInputChannel AttachedDuplexInputChannel { get { return myMessageReceiver.AttachedDuplexInputChannel; } }

        /// <summary>
        /// The method is called when a request is received.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMessageReceived(object sender, TypedSequencedRequestReceivedEventArgs<CommandInputData<_InputDataType>> e)
        {
            using (EneterTrace.Entering())
            {
                if (e.ReceivingError != null)
                {
                    EneterTrace.Error(TracedObject + " detected an error during a request receiving", e.ReceivingError.Message);
                    if (ErrorReceived != null)
                    {
                        CommandReceivingErrorEventArgs anEvent = new CommandReceivingErrorEventArgs(e.ResponseReceiverId, e.ReceivingError);

                        try
                        {
                            ErrorReceived(this, anEvent);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                        }
                    }
                }
                else
                {
                    // If execute
                    if (e.RequestMessage.Request == ECommandRequest.Execute)
                    {
                        lock (myCommandItems)
                        {
                            // Try to find command
                            // Note: The command proxy can call execute more times in case of sending the input data in fragments.
                            //       In such case the command item already exists.
                            CommandItem<_ReturnDataType, _InputDataType> aCommandItem = myCommandItems.FirstOrDefault(x => x.CommandProxyId == e.ResponseReceiverId && x.CommandId == e.RequestMessage.CommandId);

                            // If the command item does not exist then the command proxy called the execute the first time.
                            if (aCommandItem == null)
                            {
                                aCommandItem = new CommandItem<_ReturnDataType, _InputDataType>(e.ResponseReceiverId, e.RequestMessage.CommandId, myMessageReceiver);

                                lock (myConnectedCommandProxies)
                                {
                                    // Set the flag if the proxy is still connected.
                                    aCommandItem.IsCommandProxyConnected = myConnectedCommandProxies.Contains(e.ResponseReceiverId);
                                }

                                // Put the input data to the queue.
                                aCommandItem.EnqueueInputData(e.RequestMessage.InputData, e.SequenceId, e.IsSequenceCompleted);

                                myCommandItems.Add(aCommandItem);

                                // If synchronous mode is active then put the execute request to the working thread queue.
                                if (myWorkingThread != null)
                                {
                                    myWorkingThread.EnqueueMessage(aCommandItem);
                                }
                                else // If multithread mode is active then execute it in its own thread.
                                {
                                    try
                                    {
                                        myCommandProcessingDelegate.BeginInvoke(aCommandItem, CommandProcessingAsyncCallback, aCommandItem);
                                    }
                                    catch (Exception err)
                                    {
                                        // Trace the error.
                                        string aMessage = TracedObject + "failed to invoke the method processing the command.";
                                        EneterTrace.Error(aMessage, err);

                                        // Notify the error to the command proxy.
                                        aCommandItem.ResponseFailure(aMessage + " Inner exception: " + err.Message);

                                        // Remove the command from command items.
                                        myCommandItems.Remove(aCommandItem);
                                    }
                                }
                            }
                            else
                            {
                                // The command item already exists, so we must just enqueue the input data.
                                // This happens when the command proxy calls the execute more times to send the input data in fragments.
                                aCommandItem.EnqueueInputData(e.RequestMessage.InputData, e.SequenceId, e.IsSequenceCompleted);
                            }
                        }
                    }
                    else if (e.RequestMessage.Request == ECommandRequest.Pause)
                    {
                        lock (myCommandItems)
                        {
                            CommandItem<_ReturnDataType, _InputDataType> aCommandItem = myCommandItems.FirstOrDefault(x => x.CommandProxyId == e.ResponseReceiverId && x.CommandId == e.RequestMessage.CommandId);
                            if (aCommandItem != null)
                            {
                                aCommandItem.Pause();
                            }
                        }
                    }
                    else if (e.RequestMessage.Request == ECommandRequest.Resume)
                    {
                        lock (myCommandItems)
                        {
                            CommandItem<_ReturnDataType, _InputDataType> aCommandItem = myCommandItems.FirstOrDefault(x => x.CommandProxyId == e.ResponseReceiverId && x.CommandId == e.RequestMessage.CommandId);
                            if (aCommandItem != null)
                            {
                                aCommandItem.Resume();
                            }
                        }
                    }
                    else if (e.RequestMessage.Request == ECommandRequest.Cancel)
                    {
                        lock (myCommandItems)
                        {
                            CommandItem<_ReturnDataType, _InputDataType> aCommandItem = myCommandItems.FirstOrDefault(x => x.CommandProxyId == e.ResponseReceiverId && x.CommandId == e.RequestMessage.CommandId);
                            if (aCommandItem != null)
                            {
                                aCommandItem.Cancel();
                            }
                        }
                    }
                }
            }
        }

        public void PerformAttach(Action<IDuplexInputChannel> attachDelegate, IDuplexInputChannel duplexInputChannel)
        {
            using (EneterTrace.Entering())
            {
                lock (myDuplexInputChannelManipulatorLock)
                {
                    if (duplexInputChannel == null)
                    {
                        string anError = TracedObject + "failed to attach the duplex input channel because the input parameter 'duplexInputChannel' is null.";
                        EneterTrace.Error(anError);
                        throw new ArgumentNullException(anError);
                    }

                    if (string.IsNullOrEmpty(duplexInputChannel.ChannelId))
                    {
                        string anError = TracedObject + "failed to attach the duplex input channel because the input parameter 'duplexInputChannel' has null or empty channel id.";
                        EneterTrace.Error(anError);
                        throw new ArgumentException(anError);
                    }

                    if (IsDuplexInputChannelAttached)
                    {
                        string aMessage = TracedObject + "failed to attach the duplex input channel '" + duplexInputChannel.ChannelId + "' because the duplex input channel is already attached.";
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    try
                    {
                        // If the single thread mode then register the method that will read requests from the queue and execute them.
                        if (myWorkingThread != null)
                        {
                            myWorkingThread.RegisterMessageHandler(WorkingThreadHandler);
                        }

                        attachDelegate(duplexInputChannel);

                        // Store channel id for tracing purposes.
                        myDuplexInputChannelId = duplexInputChannel.ChannelId;
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + "failed to attach duplex input channel '" + duplexInputChannel.ChannelId + "'.", err);

                        // Clean attach after the failure.
                        try
                        {
                            DetachDuplexInputChannel();
                        }
                        catch
                        {
                        }

                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// The method is called when the asynchronous call to process the command is finished.
        /// (in case of parallel processing mode)
        /// </summary>
        /// <param name="asyncResult"></param>
        private void CommandProcessingAsyncCallback(IAsyncResult asyncResult)
        {
            using (EneterTrace.Entering())
            {
                CommandItem<_ReturnDataType, _InputDataType> aCommandItem = (CommandItem<_ReturnDataType, _InputDataType>)asyncResult.AsyncState;

                try
                {
                    myCommandProcessingDelegate.EndInvoke(asyncResult);
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);

                    aCommandItem.ResponseFailure(err.Message);
                }
                finally
                {
                    lock (myCommandItems)
                    {
                        myCommandItems.Remove(aCommandItem);
                    }
                }
            }
        }

        private void OnCommandProxyConnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                // Add the new connection among open connections.
                lock (myConnectedCommandProxies)
                {
                    myConnectedCommandProxies.Add(e.ResponseReceiverId);
                }

                // Notify is somebody is subscribed.
                if (CommandProxyConnected != null)
                {
                    try
                    {
                        CommandRequestEventArgs anEvent = new CommandRequestEventArgs(e.ResponseReceiverId, "");
                        CommandProxyConnected(this, anEvent);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }

        private void OnCommandProxyDisconnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                // Remove all command items for disconnected command proxy and remove it from the open connections.
                lock (myCommandItems)
                {
                    // Get all executed commands belonging to the disconnected command proxy
                    IEnumerable<CommandItem<_ReturnDataType, _InputDataType>> aCommandItems = myCommandItems.Where(x => x.CommandProxyId == e.ResponseReceiverId);
                    foreach (CommandItem<_ReturnDataType, _InputDataType> aCommandItem in aCommandItems)
                    {
                        aCommandItem.IsCommandProxyConnected = false;
                    }

                    lock (myConnectedCommandProxies)
                    {
                        myConnectedCommandProxies.Remove(e.ResponseReceiverId);
                    }
                }

                // Notify if somebody is subscribed.
                if (CommandProxyDisconnected != null)
                {
                    try
                    {
                        CommandRequestEventArgs anEvent = new CommandRequestEventArgs(e.ResponseReceiverId, "");
                        CommandProxyDisconnected(this, anEvent);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }

        /// <summary>
        /// Removes the 'execute command' requests from the queue and execute them.
        /// The method is used in case of single thread mode.
        /// </summary>
        /// <param name="commandContext"></param>
        private void WorkingThreadHandler(CommandItem<_ReturnDataType, _InputDataType> commandContext)
        {
            using (EneterTrace.Entering())
            {
                if (myCommandProcessingDelegate != null)
                {
                    try
                    {
                        myCommandProcessingDelegate(commandContext);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);

                        commandContext.ResponseFailure(err.Message);
                    }
                    finally
                    {
                        lock (myCommandItems)
                        {
                            myCommandItems.Remove(commandContext);
                        }
                    }
                }
            }
        }

        private WorkingThread<CommandItem<_ReturnDataType, _InputDataType>> myWorkingThread;

        private Action<ICommandContext<_ReturnDataType, _InputDataType>> myCommandProcessingDelegate;

        private IDuplexTypedSequencedMessageReceiver<CommandReturnData<_ReturnDataType>, CommandInputData<_InputDataType>> myMessageReceiver;

        private HashSet<string> myConnectedCommandProxies = new HashSet<string>();
        private HashSet<CommandItem<_ReturnDataType, _InputDataType>> myCommandItems = new HashSet<CommandItem<_ReturnDataType, _InputDataType>>();

        private object myDuplexInputChannelManipulatorLock = new object();

        private string myDuplexInputChannelId = "";
        private string TracedObject
        {
            get
            {
                return "The Command<" + typeof(_ReturnDataType).Name + ", " + typeof(_InputDataType).Name +" > atached to the duplex input channel '" + myDuplexInputChannelId + "' ";
            }
        }
    }
}
