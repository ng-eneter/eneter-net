/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.Composites.BufferedMessagingComposit
{
    internal class BufferedDuplexInputChannel : IDuplexInputChannel, ICompositeDuplexInputChannel
    {
        public event EventHandler<DuplexChannelMessageEventArgs> MessageReceived;

        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;

        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;


        public BufferedDuplexInputChannel(IDuplexInputChannel underlyingDuplexInputChannel, TimeSpan maxOfflineTime)
        {
            using (EneterTrace.Entering())
            {
                UnderlyingDuplexInputChannel = underlyingDuplexInputChannel;
                myMaxOfflineTime = maxOfflineTime;

                myMaxOfflineChecker = new Timer(OnMaxOfflineTimeCheckTick);
                myMaxOfflineChecker.Change(-1, -1);
            }
        }


        public string ChannelId { get { return UnderlyingDuplexInputChannel.ChannelId; } }


        public IDuplexInputChannel UnderlyingDuplexInputChannel { get; private set; }
        

        public void StartListening()
        {
            using (EneterTrace.Entering())
            {
                lock (myListeningManipulatorLock)
                {
                    if (IsListening)
                    {
                        string aMessage = TracedObject + ErrorHandler.IsAlreadyListening;
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    UnderlyingDuplexInputChannel.ResponseReceiverConnected += OnResponseReceiverConnected;
                    UnderlyingDuplexInputChannel.MessageReceived += OnMessageReceived;

                    try
                    {
                        UnderlyingDuplexInputChannel.StartListening();
                    }
                    catch (Exception err)
                    {
                        UnderlyingDuplexInputChannel.ResponseReceiverConnected -= OnResponseReceiverConnected;
                        UnderlyingDuplexInputChannel.MessageReceived -= OnMessageReceived;

                        EneterTrace.Error(TracedObject + ErrorHandler.StartListeningFailure, err);
                    }

                    myMaxOfflineCheckerRequestedToStop = false;
                }
            }
        }


        public void StopListening()
        {
            using (EneterTrace.Entering())
            {
                lock (myListeningManipulatorLock)
                {
                    // Indicate, that the timer responsible for checking if response receivers are timeouted (i.e. exceeded the max offline time)
                    // shall stop.
                    myMaxOfflineCheckerRequestedToStop = true;

                    try
                    {
                        UnderlyingDuplexInputChannel.StopListening();
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.StopListeningFailure, err);
                    }

                    UnderlyingDuplexInputChannel.ResponseReceiverConnected -= OnResponseReceiverConnected;
                    UnderlyingDuplexInputChannel.MessageReceived -= OnMessageReceived;
                }
            }
        }

        public bool IsListening
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    lock (myListeningManipulatorLock)
                    {
                        return UnderlyingDuplexInputChannel.IsListening;
                    }
                }
            }
        }

        public void SendResponseMessage(string responseReceiverId, object message)
        {
            using (EneterTrace.Entering())
            {
                ResponseReceiverContext aResponseReceiverContext;

                lock (myResponseReceivers)
                {
                    aResponseReceiverContext = myResponseReceivers.FirstOrDefault(x => x.ResponseReceiverId == responseReceiverId);

                    // If the response receiver was not found, then it means, that the response receiver is not connected.
                    // In order to support independent startup order, we can suppose, the response receiver can connect later
                    // and the response messages will be then delivered.
                    // If not, then response messages will be deleted automatically after the timeout (maxOfflineTime).
                    if (aResponseReceiverContext == null)
                    {
                        // Create the response receiver context - it allows to enqueue response messages before connection of
                        // the response receiver.
                        aResponseReceiverContext = new ResponseReceiverContext(responseReceiverId, UnderlyingDuplexInputChannel, UpdateLastActivity);
                        myResponseReceivers.Add(aResponseReceiverContext);

                        // If it is the first response receiver, then start the timer checking which response receivers
                        // are disconnected due to the timeout (i.e. max offline time)
                        if (myResponseReceivers.Count == 1)
                        {
                            myMaxOfflineChecker.Change(300, -1);
                        }

                    }
                }

                // Enqueue the message.
                aResponseReceiverContext.SendResponseMessage(message);
            }
        }

        public void DisconnectResponseReceiver(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                lock (myResponseReceivers)
                {
                    ResponseReceiverContext aResponseReceiverContext = myResponseReceivers.FirstOrDefault(x => x.ResponseReceiverId == responseReceiverId);
                    if (aResponseReceiverContext != null)
                    {
                        aResponseReceiverContext.StopSendingOfResponseMessages();
                    }
                }

                UnderlyingDuplexInputChannel.DisconnectResponseReceiver(responseReceiverId);
            }
        }

        private void OnResponseReceiverConnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                // Update the time for the response receiver.
                // If the response receiver does not exist, then create it.
                UpdateLastActivity(e.ResponseReceiverId, true);

                if (ResponseReceiverConnected != null)
                {
                    try
                    {
                        ResponseReceiverConnected(this, e);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }



        private void OnMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                // Update the time for the response receiver.
                // If the response receiver does not exist, then create it.
                UpdateLastActivity(e.ResponseReceiverId, true);

                if (MessageReceived != null)
                {
                    try
                    {
                        MessageReceived(this, e);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }


        private void UpdateLastActivity(string responseReceiverId, bool createNewIfDoesNotExistFlag)
        {
            using (EneterTrace.Entering())
            {
                lock (myResponseReceivers)
                {
                    ResponseReceiverContext aResponseReceiverContext = myResponseReceivers.FirstOrDefault(x => x.ResponseReceiverId == responseReceiverId);

                    // If the response receiver was not found, then it means, that the response receiver is not connected.
                    // In order to support independent startup order, we can suppose, the response receiver can connect later
                    // and the response messages will be then dlivered.
                    // If not, then response messages will be deleted automatically after the timeout (maxOfflineTime).
                    if (aResponseReceiverContext == null && createNewIfDoesNotExistFlag)
                    {
                        // Create the response receiver context - it allows to enqueue response messages before connection of
                        // the response receiver.
                        aResponseReceiverContext = new ResponseReceiverContext(responseReceiverId, UnderlyingDuplexInputChannel, UpdateLastActivity);
                        myResponseReceivers.Add(aResponseReceiverContext);

                        // If it is the first response receiver, then start the timer checking which response receivers
                        // are disconnected due to the timeout (i.e. max offline time)
                        if (myResponseReceivers.Count == 1)
                        {
                            myMaxOfflineChecker.Change(300, -1);
                        }
                    }

                    // Update time of the last response receiver activity.
                    if (aResponseReceiverContext != null)
                    {
                        aResponseReceiverContext.UpdateLastActivityTime();
                    }
                }
            }
        }

        private void OnMaxOfflineTimeCheckTick(object o)
        {
            using (EneterTrace.Entering())
            {
                // Do nothing if there is a request to stop.
                if (myMaxOfflineCheckerRequestedToStop)
                {
                    return;
                }

                List<ResponseReceiverContext> aTimeoutedResponseReceivers = new List<ResponseReceiverContext>();

                DateTime aCurrentCheckTime = DateTime.Now;
                bool aTimerShallContinueFlag;

                lock (myResponseReceivers)
                {
                    myResponseReceivers.RemoveWhere(x =>
                        {
                            if (aCurrentCheckTime - x.LastActivityTime > myMaxOfflineTime)
                            {
                                aTimeoutedResponseReceivers.Add(x);

                                // Indicate, the response receiver can be removed.
                                return true;
                            }

                            // Response receiver will not be removed.
                            return false;
                        });

                    aTimerShallContinueFlag = myResponseReceivers.Count > 0;
                }

                // Do nothing if there is a request to stop.
                if (myMaxOfflineCheckerRequestedToStop)
                {
                    return;
                }

                // Notify disconnected response receivers.
                foreach (ResponseReceiverContext aResponseReceiverContext in aTimeoutedResponseReceivers)
                {
                    aResponseReceiverContext.StopSendingOfResponseMessages();

                    // Try to disconnect the response receiver.
                    try
                    {
                        UnderlyingDuplexInputChannel.DisconnectResponseReceiver(aResponseReceiverContext.ResponseReceiverId);
                    }
                    catch
                    {
                        // The exception could occur because the response receiver is not connected.
                        // It is ok.
                    }

                    if (ResponseReceiverDisconnected != null)
                    {
                        try
                        {
                            ResponseReceiverEventArgs aMsg = new ResponseReceiverEventArgs(aResponseReceiverContext.ResponseReceiverId);
                            ResponseReceiverDisconnected(this, aMsg);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                        }
                    }
                }

                // If the timer checking the timeout for response receivers shall continue
                if (!myMaxOfflineCheckerRequestedToStop && aTimerShallContinueFlag)
                {
                    myMaxOfflineChecker.Change(300, -1);
                }
            }
        }


        private object myListeningManipulatorLock = new object();
        
        private TimeSpan myMaxOfflineTime;
        private Timer myMaxOfflineChecker;
        private bool myMaxOfflineCheckerRequestedToStop;

        private HashSet<ResponseReceiverContext> myResponseReceivers = new HashSet<ResponseReceiverContext>();


        private string TracedObject
        {
            get
            {
                string aChannelId = (UnderlyingDuplexInputChannel != null) ? UnderlyingDuplexInputChannel.ChannelId : "";
                return "Buffered duplex input channel '" + aChannelId + "' ";
            }
        }
    }
}
