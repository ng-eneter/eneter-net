/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/


using System;
using System.Collections.Generic;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase
{
    internal class DefaultDuplexInputChannel : IDuplexInputChannel
    {
        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;
        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;
        public event EventHandler<DuplexChannelMessageEventArgs> MessageReceived;

        public DefaultDuplexInputChannel(string channelId,  // address to listen
            IThreadDispatcher dispatcher,                         // threading model used to notify messages and events
            IThreadDispatcher dispatchingAfterMessageReading,
            IInputConnector inputConnector)                 // listener used for listening to messages
        {
            using (EneterTrace.Entering())
            {
                if (string.IsNullOrEmpty(channelId))
                {
                    EneterTrace.Error(ErrorHandler.NullOrEmptyChannelId);
                    throw new ArgumentException(ErrorHandler.NullOrEmptyChannelId);
                }

                ChannelId = channelId;

                Dispatcher = dispatcher;

                // Internal dispatcher used when the message is decoded.
                // E.g. Shared memory messaging needs to return looping immediately the protocol message is decoded
                //      so that other senders are not blocked.
                myDispatchingAfterMessageReading = dispatchingAfterMessageReading;

                myInputConnector = inputConnector;
            }
        }

        public string ChannelId { get; private set; }

        public void StartListening()
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myListeningManipulatorLock))
                {
                    // If the channel is already listening.
                    if (IsListening)
                    {
                        string aMessage = TracedObject + ErrorHandler.IsAlreadyListening;
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    try
                    {
                        // Start listen to messages.
                        myInputConnector.StartListening(HandleMessage);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.FailedToStartListening, err);

                        // The listening did not start correctly.
                        // So try to clean.
                        StopListening();

                        throw;
                    }
                }
            }
        }

        public void StopListening()
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myListeningManipulatorLock))
                {
                    try
                    {
                        // Try to close connected clients.
                        DisconnectClients();
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + "failed to disconnect connected clients.", err);
                    }

                    try
                    {
                        myInputConnector.StopListening();
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.IncorrectlyStoppedListening, err);
                    }
                }
            }
        }

        public bool IsListening
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    using (ThreadLock.Lock(myListeningManipulatorLock))
                    {
                        return myInputConnector.IsListening;
                    }
                }
            }
        }


        public void SendResponseMessage(string responseReceiverId, object message)
        {
            using (EneterTrace.Entering())
            {
                if (string.IsNullOrEmpty(responseReceiverId))
                {
                    string aMessage = TracedObject + "detected the input parameter responseReceiverId is null or empty string.";
                    EneterTrace.Error(aMessage);
                    throw new ArgumentException(aMessage);
                }

                if (!IsListening)
                {
                    string aMessage = TracedObject + ErrorHandler.FailedToSendResponseBecauseNotListening;
                    EneterTrace.Error(aMessage);
                    throw new InvalidOperationException(aMessage);
                }

                // If broadcast to all connected response receivers.
                if (responseReceiverId == "*")
                {
                    List<string> aDisconnectedClients = new List<string>();

                    using (ThreadLock.Lock(myConnectedClients))
                    {
                        // Send the response message to all connected clients.
                        foreach (KeyValuePair<string, string> aConnectedClient in myConnectedClients)
                        {
                            try
                            {
                                // Send the response message.
                                myInputConnector.SendResponseMessage(aConnectedClient.Key, message);
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Error(TracedObject + ErrorHandler.FailedToSendResponseMessage, err);
                                aDisconnectedClients.Add(aConnectedClient.Key);

                                // Note: Exception is not rethrown because if sending to one client fails it should not
                                //       affect sending to other clients.
                            }
                        }
                    }

                    // Disconnect failed clients.
                    foreach (String aResponseReceiverId in aDisconnectedClients)
                    {
                        CloseConnection(aResponseReceiverId, true, true);
                    }
                }
                else
                {
                    try
                    {
                        // Send the response message.
                        myInputConnector.SendResponseMessage(responseReceiverId, message);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.FailedToSendResponseMessage, err);
                        CloseConnection(responseReceiverId, true, true);
                        throw;
                    }
                }
            }
        }

        public void DisconnectResponseReceiver(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                CloseConnection(responseReceiverId, true, false);
            }
        }


        public IThreadDispatcher Dispatcher { get; private set; }

        private void DisconnectClients()
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myConnectedClients))
                {
                    foreach (KeyValuePair<string, string> aConnection in myConnectedClients)
                    {
                        // Note: we must assign receiver id (and sender address) to a separate variable because aConnection is the same instance for all iterations.
                        //       And so the client could get incorrect id in the notified event.
                        string aResponseReceiverId = aConnection.Key;

                        try
                        {
                            myInputConnector.CloseConnection(aResponseReceiverId);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.FailedToCloseConnection, err);
                        }
                    }
                    myConnectedClients.Clear();
                }
            }
        }

        // Note: This method is called from the message receiving thread.
        private void HandleMessage(MessageContext messageContext)
        {
            using (EneterTrace.Entering())
            {
                if (messageContext.ProtocolMessage.MessageType == EProtocolMessageType.MessageReceived)
                {
                    EneterTrace.Debug("REQUEST MESSAGE RECEIVED");

                    myDispatchingAfterMessageReading.Invoke(() =>
                        {
                            // If the connection is not open then open it.
                            //OpenConnection(messageContext.ProtocolMessage.ResponseReceiverId, messageContext.SenderAddress);
                            Dispatcher.Invoke(() => NotifyMessageReceived(messageContext, messageContext.ProtocolMessage));
                        });
                }
                else if (messageContext.ProtocolMessage.MessageType == EProtocolMessageType.OpenConnectionRequest)
                {
                    EneterTrace.Debug("CLIENT CONNECTION RECEIVED");
                    myDispatchingAfterMessageReading.Invoke(() => OpenConnection(messageContext.ProtocolMessage.ResponseReceiverId, messageContext.SenderAddress));
                }
                else if (messageContext.ProtocolMessage.MessageType == EProtocolMessageType.CloseConnectionRequest)
                {
                    EneterTrace.Debug("CLIENT DISCONNECTION RECEIVED");
                    myDispatchingAfterMessageReading.Invoke(() => CloseConnection(messageContext.ProtocolMessage.ResponseReceiverId, false, true));
                }
                else
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.FailedToReceiveMessageBecauseIncorrectFormat);
                }
            }
        }

        /// <summary>
        /// Creates the connection if does not exist.
        /// Returns false if the opening the connection was not approved - user rejected the connection via the connection token.
        /// </summary>
        /// <param name="responseReceiverId"></param>
        /// <param name="senderAddress"></param>
        /// <returns></returns>
        private void OpenConnection(string responseReceiverId, string senderAddress)
        {
            using (EneterTrace.Entering())
            {
                bool aNewConnectionFlag = false;

                // If the connection is not open yet.
                using (ThreadLock.Lock(myConnectedClients))
                {
                    if (!myConnectedClients.ContainsKey(responseReceiverId))
                    {
                        myConnectedClients[responseReceiverId] = senderAddress;

                        // Connection was created.
                        aNewConnectionFlag = true;
                    }
                }

                if (aNewConnectionFlag)
                {
                    // Open connection comes from the client. So notify it from dispatcher threading.
                    Dispatcher.Invoke(() => Notify(ResponseReceiverConnected, responseReceiverId, senderAddress));
                }
            }
        }

        private void CloseConnection(string responseReceiverId, bool sendCloseMessageFlag, bool notifyDisconnectFlag)
        {
            using (EneterTrace.Entering())
            {
                string aSenderAddress = "";
                bool aConnecionExisted = false;
                try
                {
                    using (ThreadLock.Lock(myConnectedClients))
                    {
                        myConnectedClients.TryGetValue(responseReceiverId, out aSenderAddress);
                        aConnecionExisted = myConnectedClients.Remove(responseReceiverId);
                    }

                    if (aConnecionExisted && sendCloseMessageFlag)
                    {
                        try
                        {
                            // Try to send close connection message.
                            myInputConnector.CloseConnection(responseReceiverId);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.FailedToCloseConnection, err);
                        }
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + "failed to close connection with response receiver.", err);
                }

                // If a connection was closed and notification event is required.
                if (aConnecionExisted && notifyDisconnectFlag)
                {
                    // Notify the connection was closed.
                    Dispatcher.Invoke(() => Notify(ResponseReceiverDisconnected, responseReceiverId, aSenderAddress));
                }
            }
        }

        private void Notify(EventHandler<ResponseReceiverEventArgs> handler, string responseReceiverId, string senderAddress)
        {
            using (EneterTrace.Entering())
            {
                ResponseReceiverEventArgs anEventArgs = new ResponseReceiverEventArgs(responseReceiverId, senderAddress);
                NotifyGeneric<ResponseReceiverEventArgs>(handler, anEventArgs, false);
            }
        }


        private void NotifyMessageReceived(MessageContext messageContext, ProtocolMessage protocolMessage)
        {
            using (EneterTrace.Entering())
            {
                DuplexChannelMessageEventArgs anEventArgs = new DuplexChannelMessageEventArgs(ChannelId, protocolMessage.Message, protocolMessage.ResponseReceiverId, messageContext.SenderAddress);
                NotifyGeneric<DuplexChannelMessageEventArgs>(MessageReceived, anEventArgs, true);
            }
        }

        private void NotifyGeneric<T>(EventHandler<T> handler, T eventArgs, bool isNobodySubscribedWarning)
            where T : EventArgs
        {
            using (EneterTrace.Entering())
            {
                if (handler != null)
                {
                    try
                    {
                        handler(this, eventArgs);
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


        // Key = responseReceiverId, Value = senderAddress
        private Dictionary<string, string> myConnectedClients = new Dictionary<string, string>();

        private IThreadDispatcher myDispatchingAfterMessageReading;
        private IInputConnector myInputConnector;

        private object myListeningManipulatorLock = new object();

        private string TracedObject
        {
            get
            {
                return GetType().Name + " '" + ChannelId + "' ";
            }
        }
    }
}
