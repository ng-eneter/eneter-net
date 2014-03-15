/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase
{
    internal class DefaultDuplexInputChannel : IDuplexInputChannel
    {
        // Represents connection with a client.
        private class TConnectionContext
        {
            public TConnectionContext(ISender responseSender, string senderAddress)
            {
                ResponseSender = responseSender;
                SenderAddress = senderAddress;
            }

            public string SenderAddress { get; private set; }
            public ISender ResponseSender { get; private set; }
        }

        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;
        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;
        public event EventHandler<DuplexChannelMessageEventArgs> MessageReceived;

        public DefaultDuplexInputChannel(string channelId,  // address to listen
            IThreadDispatcher dispatcher,                         // threading model used to notify messages and events
            IThreadDispatcher dispatchingAfterMessageReading,
            IInputConnector inputConnector,                 // listener used for listening to messages
            IProtocolFormatter protocolFormatter)           // how messages are encoded between channels
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

                myProtocolFormatter = protocolFormatter;
                myInputConnector = inputConnector;

                IncludeResponseReceiverIdToResponses = false;
            }
        }

        public bool IncludeResponseReceiverIdToResponses { private get; set; }

        public string ChannelId { get; private set; }

        public void StartListening()
        {
            using (EneterTrace.Entering())
            {
                lock (myListeningManipulatorLock)
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
                        EneterTrace.Error(TracedObject + ErrorHandler.StartListeningFailure, err);

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
                lock (myListeningManipulatorLock)
                {
                    myStopListeningIsRunning = true;

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
                        EneterTrace.Warning(TracedObject + ErrorHandler.StopListeningFailure, err);
                    }

                    myStopListeningIsRunning = false;
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
                        return myInputConnector.IsListening;
                    }
                }
            }
        }


        public void SendResponseMessage(string responseReceiverId, object message)
        {
            using (EneterTrace.Entering())
            {
                if (!IsListening)
                {
                    string aMessage = TracedObject + ErrorHandler.SendResponseNotListeningFailure;
                    EneterTrace.Error(aMessage);
                    throw new InvalidOperationException(aMessage);
                }

                lock (myConnectedClients)
                {
                    // Try to find the response sender
                    TConnectionContext aConnectionContext;
                    myConnectedClients.TryGetValue(responseReceiverId, out aConnectionContext);
                    if (aConnectionContext == null)
                    {
                        string aMessage = TracedObject + ErrorHandler.SendResponseNotConnectedFailure;
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    try
                    {
                        // Send the response message.
                        string aResponseReceiverId = IncludeResponseReceiverIdToResponses ? responseReceiverId : "";
                        SenderUtil.SendMessage(aConnectionContext.ResponseSender, aResponseReceiverId, message, myProtocolFormatter);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.SendResponseFailure, err);

                        CloseResponseMessageSender(responseReceiverId, true);

                        throw;
                    }
                }
            }
        }

        public void DisconnectResponseReceiver(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                CloseResponseMessageSender(responseReceiverId, true);
            }
        }

        public IThreadDispatcher Dispatcher { get; private set; }

        private void DisconnectClients()
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectedClients)
                {
                    foreach (KeyValuePair<string, TConnectionContext> aConnection in myConnectedClients)
                    {
                        if (aConnection.Value.ResponseSender is IDisposable)
                        {
                            ((IDisposable)aConnection.Value.ResponseSender).Dispose();
                        }

                        // Note: we must assign receiver id to a separate variable because aConnection is same instance for all iterations.
                        //       And so the client could get incorrect id in the notified event.
                        string aResponseReceiverId = aConnection.Key;
                        Dispatcher.Invoke(() => Notify(ResponseReceiverDisconnected, aResponseReceiverId, aConnection.Value.SenderAddress));
                    }
                    myConnectedClients.Clear();
                }
            }
        }

        // Note: This method is called from the message receiving thread.
        private bool HandleMessage(MessageContext messageContext)
        {
            using (EneterTrace.Entering())
            {
                // If the listening thread stopped looping.
                if (messageContext == null)
                {
                    // If the listening stopped because it was requested.
                    if (myStopListeningIsRunning)
                    {
                        return false;
                    }

                    // The listening failed.
                    EneterTrace.Warning(TracedObject + "detected the listening was stopped.");

                    // Try to correctly stop the whole listening.
                    StopListening();
                    return false;
                }

                // Get the protocol message from incoming data.
                ProtocolMessage aProtocolMessage = null;
                if (messageContext.Message != null)
                {
                    if (messageContext.Message is Stream)
                    {
                        aProtocolMessage = myProtocolFormatter.DecodeMessage((Stream)messageContext.Message);
                    }
                    else
                    {
                        aProtocolMessage = myProtocolFormatter.DecodeMessage(messageContext.Message);
                    }
                }

                // If some client disconnected from the server without sending the close message.
                if (aProtocolMessage == null)
                {
                    // Try to correctly close the client.
                    myDispatchingAfterMessageReading.Invoke(() => CleanAfterDisconnectedResponseReceiver(messageContext.ResponseSender, messageContext.SenderAddress));

                    // Stop listening for this client.
                    return false;
                }
                else if (aProtocolMessage.MessageType == EProtocolMessageType.MessageReceived)
                {
                    EneterTrace.Debug("REQUEST MESSAGE RECEIVED");

                    myDispatchingAfterMessageReading.Invoke(() =>
                        {
                            // If the connection is not open then it will open it.
                            CreateResponseMessageSender(messageContext, aProtocolMessage.ResponseReceiverId);
                            Dispatcher.Invoke(() => NotifyMessageReceived(messageContext, aProtocolMessage));
                        });
                }
                else if (aProtocolMessage.MessageType == EProtocolMessageType.OpenConnectionRequest)
                {
                    EneterTrace.Debug("CLIENT CONNECTION RECEIVED");
                    myDispatchingAfterMessageReading.Invoke(() => CreateResponseMessageSender(messageContext, aProtocolMessage.ResponseReceiverId));
                }
                else if (aProtocolMessage.MessageType == EProtocolMessageType.CloseConnectionRequest)
                {
                    EneterTrace.Debug("CLIENT DISCONNECTION RECEIVED");
                    myDispatchingAfterMessageReading.Invoke(() => CloseResponseMessageSender(aProtocolMessage.ResponseReceiverId, false));
                    return false;
                }
                else
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.ReceiveMessageIncorrectFormatFailure);
                }

                return true;
            }
        }

        private void CleanAfterDisconnectedResponseReceiver(ISender responseSender, string senderAddress)
        {
            using (EneterTrace.Entering())
            {
                // Some client got disconnected without sending the 'close' messages.
                // So try to identify closed response receiver and clean it.
                KeyValuePair<string, TConnectionContext> aPair;
                lock (myConnectedClients)
                {
                    // Note: KeyValuePair is a struct so the default value is not null.
                    aPair = myConnectedClients.FirstOrDefault(x => x.Value.ResponseSender == responseSender);
                }

                if (!String.IsNullOrEmpty(aPair.Key))
                {
                    CloseResponseMessageSender(aPair.Key, false);
                }
                else
                {
                    // Decoding the message returned null.
                    // This can happen if a client closed the connection and the stream was closed.
                    // e.g. in case of Named Pipes.

                    // nothing to do here.
                }
            }
        }


        /// <summary>
        /// Creates the connection if does not exist.
        /// Returns false if the opening the connection was not approved - user rejected the connection via the connection token.
        /// </summary>
        /// <param name="messageContext"></param>
        /// <param name="responseReceiverId"></param>
        /// <returns></returns>
        private void CreateResponseMessageSender(MessageContext messageContext, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                bool aNewConnectionFlag = false;

                // If the connection is not open yet.
                TConnectionContext aConnectionContext;
                lock (myConnectedClients)
                {
                    myConnectedClients.TryGetValue(responseReceiverId, out aConnectionContext);

                    if (aConnectionContext == null)
                    {
                        // Get the response sender.
                        // Some messagings send the response sender in the message context and some
                        // provides the factory to create it.
                        ISender aResponseSender = messageContext.ResponseSender;
                        if (aResponseSender == null)
                        {
                            aResponseSender = myInputConnector.CreateResponseSender(responseReceiverId);
                        }

                        aConnectionContext = new TConnectionContext(aResponseSender, messageContext.SenderAddress);
                        myConnectedClients[responseReceiverId] = aConnectionContext;

                        // Connection was created.
                        aNewConnectionFlag = true;
                    }
                }

                if (aNewConnectionFlag)
                {
                    // Open connection comes from the client. So notify it from dispatcher threading.
                    Dispatcher.Invoke(() => Notify(ResponseReceiverConnected, responseReceiverId, aConnectionContext.SenderAddress));
                }
            }
        }

        private void CloseResponseMessageSender(string responseReceiverId, bool sendCloseMessageFlag)
        {
            using (EneterTrace.Entering())
            {
                TConnectionContext aConnectionContext = null;

                try
                {
                    lock (myConnectedClients)
                    {
                        myConnectedClients.TryGetValue(responseReceiverId, out aConnectionContext);
                        myConnectedClients.Remove(responseReceiverId);
                    }

                    if (aConnectionContext != null)
                    {
                        if (sendCloseMessageFlag)
                        {
                            try
                            {
                                // Try to send close connection message.
                                string aResponseReceiverId = IncludeResponseReceiverIdToResponses ? responseReceiverId : "";
                                SenderUtil.SendCloseConnection(aConnectionContext.ResponseSender, aResponseReceiverId, myProtocolFormatter);
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.CloseConnectionFailure, err);
                            }
                        }

                        if (aConnectionContext.ResponseSender is IDisposable)
                        {
                            ((IDisposable) aConnectionContext.ResponseSender).Dispose();
                        }
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + "failed to close connection with response receiver.", err);
                }

                // If a connection was closed.
                if (aConnectionContext != null)
                {
                    // Notify the connection was closed.
                    Dispatcher.Invoke(() => Notify(ResponseReceiverDisconnected, responseReceiverId, aConnectionContext.SenderAddress));
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


        private Dictionary<string, TConnectionContext> myConnectedClients = new Dictionary<string, TConnectionContext>();

        private IThreadDispatcher myDispatchingAfterMessageReading;
        private IInputConnector myInputConnector;
        private IProtocolFormatter myProtocolFormatter;

        private bool myStopListeningIsRunning;
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
