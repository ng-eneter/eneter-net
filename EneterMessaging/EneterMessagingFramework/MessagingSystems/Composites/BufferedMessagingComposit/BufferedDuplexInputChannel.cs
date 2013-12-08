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
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.MessagingSystems.Composites.BufferedMessagingComposit
{
    internal class BufferedDuplexInputChannel : IDuplexInputChannel
    {
        public event EventHandler<ConnectionTokenEventArgs> ResponseReceiverConnecting;
        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;
        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;
        public event EventHandler<DuplexChannelMessageEventArgs> MessageReceived;


        public BufferedDuplexInputChannel(IDuplexInputChannel underlyingDuplexInputChannel, TimeSpan maxOfflineTime)
        {
            using (EneterTrace.Entering())
            {
                myUnderlyingInputChannel = underlyingDuplexInputChannel;
                myMaxOfflineTime = maxOfflineTime;

                myMaxOfflineChecker = new Timer(OnMaxOfflineTimeCheckTick, null, -1, -1);
            }
        }


        public string ChannelId { get { return myUnderlyingInputChannel.ChannelId; } }

        public void StartListening()
        {
            using (EneterTrace.Entering())
            {
                lock (myListeningManipulatorLock)
                {
                    myUnderlyingInputChannel.ResponseReceiverConnecting += OnResponseReceiverConnecting;
                    myUnderlyingInputChannel.ResponseReceiverConnected += OnResponseReceiverConnected;
                    myUnderlyingInputChannel.ResponseReceiverDisconnected += OnResponseReceiverDisconnected;
                    myUnderlyingInputChannel.MessageReceived += OnMessageReceived;

                    try
                    {
                        myUnderlyingInputChannel.StartListening();
                    }
                    catch (Exception err)
                    {
                        myUnderlyingInputChannel.ResponseReceiverConnecting -= OnResponseReceiverConnecting;
                        myUnderlyingInputChannel.ResponseReceiverConnected -= OnResponseReceiverConnected;
                        myUnderlyingInputChannel.ResponseReceiverDisconnected -= OnResponseReceiverDisconnected;
                        myUnderlyingInputChannel.MessageReceived -= OnMessageReceived;

                        EneterTrace.Error(TracedObject + ErrorHandler.StartListeningFailure, err);
                        throw;
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

                    lock (myResponseReceivers)
                    {
                        // Stop buffering for all connected response receivers.
                        foreach (ResponseReceiverContext aResponseReceiverContext in myResponseReceivers)
                        {
                            aResponseReceiverContext.StopSendingOfResponseMessages();
                        }

                        myResponseReceivers.Clear();
                    }

                    try
                    {
                        myUnderlyingInputChannel.StopListening();
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.StopListeningFailure, err);
                    }

                    myUnderlyingInputChannel.ResponseReceiverConnecting -= OnResponseReceiverConnecting;
                    myUnderlyingInputChannel.ResponseReceiverConnected -= OnResponseReceiverConnected;
                    myUnderlyingInputChannel.ResponseReceiverDisconnected -= OnResponseReceiverDisconnected;
                    myUnderlyingInputChannel.MessageReceived -= OnMessageReceived;
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
                        return myUnderlyingInputChannel.IsListening;
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
                        // Note: The client address is not known yet.
                        aResponseReceiverContext = new ResponseReceiverContext(responseReceiverId, "", myUnderlyingInputChannel);
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
                        // Stop the buffer queue.
                        aResponseReceiverContext.StopSendingOfResponseMessages();

                        // Remove the receiver from the list.
                        myResponseReceivers.Remove(aResponseReceiverContext);
                    }
                }

                myUnderlyingInputChannel.DisconnectResponseReceiver(responseReceiverId);
            }
        }

        public IDispatcher Dispatcher { get { return myUnderlyingInputChannel.Dispatcher; } }

        private void OnResponseReceiverConnecting(object sender, ConnectionTokenEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                Notify<ConnectionTokenEventArgs>(ResponseReceiverConnecting, () => e, false);
            }
        }

        private void OnResponseReceiverConnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                // Update that the response receiver is connected.
                // If the response receiver does not exist, then create it.
                UpdateResponseReceiverContext(e.ResponseReceiverId, e.SenderAddress, true, true);

                Notify<ResponseReceiverEventArgs>(ResponseReceiverConnected, () => e, false);
            }
        }

        private void OnResponseReceiverDisconnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                // Update that the response receiver is disconnected.
                UpdateResponseReceiverContext(e.ResponseReceiverId, e.SenderAddress, false, false);
            }
        }

        private void OnMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                // Update the time for the response receiver.
                // If the response receiver does not exist, then create it.
                UpdateResponseReceiverContext(e.ResponseReceiverId, e.SenderAddress, true, true);

                Notify<DuplexChannelMessageEventArgs>(MessageReceived, () => e, true);
            }
        }


        private void UpdateResponseReceiverContext(string responseReceiverId, string clientAddress, bool isConnected, bool createNewIfDoesNotExistFlag)
        {
            using (EneterTrace.Entering())
            {
                lock (myResponseReceivers)
                {
                    ResponseReceiverContext aResponseReceiverContext = myResponseReceivers.FirstOrDefault(x => x.ResponseReceiverId == responseReceiverId);

                    // If the response receiver was not found, then it means, that the response receiver is not connected.
                    // In order to support independent startup order, we can suppose, the response receiver can connect later
                    // and the response messages will be then delivered.
                    // If not, then response messages will be deleted automatically after the timeout (maxOfflineTime).
                    if (aResponseReceiverContext == null && createNewIfDoesNotExistFlag)
                    {
                        // Create the response receiver context - it allows to enqueue response messages before connection of
                        // the response receiver.
                        aResponseReceiverContext = new ResponseReceiverContext(responseReceiverId, clientAddress, myUnderlyingInputChannel);
                        aResponseReceiverContext.SetConnectionState(isConnected);
                        myResponseReceivers.Add(aResponseReceiverContext);

                        // If it is the first response receiver, then start the timer checking which response receivers
                        // are disconnected due to the timeout (i.e. max offline time)
                        if (myResponseReceivers.Count == 1)
                        {
                            myMaxOfflineChecker.Change(300, -1);
                        }
                    }

                    // Update the connection status.
                    if (aResponseReceiverContext != null)
                    {
                        aResponseReceiverContext.SetConnectionState(isConnected);

                        if (!String.IsNullOrEmpty(clientAddress))
                        {
                            aResponseReceiverContext.ClientAddress = clientAddress;
                        }
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
                    // Remove all not connected response receivers which exceeded the max offline timeout.
                    myResponseReceivers.RemoveWhere(x =>
                        {
                            // If disconnected and max offline time is exceeded. 
                            if (!x.IsResponseReceiverConnected &&
                                aCurrentCheckTime - x.LastConnectionChangeTime > myMaxOfflineTime)
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

                // Notify disconnected response receivers.
                foreach (ResponseReceiverContext aResponseReceiverContext in aTimeoutedResponseReceivers)
                {
                    // Stop disconnecting if the we are requested to stop.
                    if (myMaxOfflineCheckerRequestedToStop)
                    {
                        return;
                    }

                    aResponseReceiverContext.StopSendingOfResponseMessages();

                    // Try to disconnect the response receiver.
                    try
                    {
                        myUnderlyingInputChannel.DisconnectResponseReceiver(aResponseReceiverContext.ResponseReceiverId);
                    }
                    catch
                    {
                        // The exception could occur because the response receiver is not connected.
                        // It is ok.
                    }

                    // Invoke the event in the correct thread.
                    Dispatcher.Invoke(() => Notify<ResponseReceiverEventArgs>(ResponseReceiverDisconnected, () => new ResponseReceiverEventArgs(aResponseReceiverContext.ResponseReceiverId, aResponseReceiverContext.ClientAddress), false));
                }

                // If the timer checking the timeout for response receivers shall continue
                if (!myMaxOfflineCheckerRequestedToStop && aTimerShallContinueFlag)
                {
                    myMaxOfflineChecker.Change(300, -1);
                }
            }
        }

        private void Notify<T>(EventHandler<T> handler, Func<T> eventFactory, bool isNobodySubscribedWarning)
            where T : EventArgs
        {
            using (EneterTrace.Entering())
            {
                if (handler != null)
                {
                    try
                    {
                        T anEventArgs = eventFactory();
                        handler(this, anEventArgs);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
                else if (isNobodySubscribedWarning)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.NobodySubscribedForMessage);
                }
            }
        }


        private object myListeningManipulatorLock = new object();
        
        private TimeSpan myMaxOfflineTime;
        private Timer myMaxOfflineChecker;
        private bool myMaxOfflineCheckerRequestedToStop;
        private IDuplexInputChannel myUnderlyingInputChannel;

        private HashSet<ResponseReceiverContext> myResponseReceivers = new HashSet<ResponseReceiverContext>();


        private string TracedObject
        {
            get
            {
                return GetType().Name + " '" + ChannelId + "' ";
            }
        }
    }
}
