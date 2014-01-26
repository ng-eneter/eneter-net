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
                myProtocolFormatter = protocolFormatter;
                myInputConnector = inputConnector;
            }
        }

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
                        SenderUtil.SendMessage(aConnectionContext.ResponseSender, "", message, myProtocolFormatter);
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

                        Dispatcher.Invoke(() => Notify(ResponseReceiverDisconnected, aConnection.Key, aConnection.Value.SenderAddress));
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
                    // Try to correctly stop the whole listening.
                    // Note: Use a different thread because 'StopListening' will try to stop
                    //       this message listening thread and so the deadlock would occur.
                    ThreadPool.QueueUserWorkItem(x => StopListening());

                    EneterTrace.Warning(TracedObject + "detected the listening was stopped.");

                    return false;
                }

                ProtocolMessage aProtocolMessage = null;

                // if there are message data available.
                if (messageContext.Message != null)
                {
                    aProtocolMessage = GetProtocolMessage(messageContext.Message);
                }

                // If some client disconnected from the server without sending the close message.
                if (aProtocolMessage == null)
                {
                    // Try to correctly close the client.
                    ThreadPool.QueueUserWorkItem(x => CleanDisconnectedResponseReceiver(messageContext.ResponseSender, messageContext.SenderAddress));

                    // Stop listening for this client.
                    return false;
                }

                if (aProtocolMessage.MessageType == EProtocolMessageType.MessageReceived)
                {
                    // If the connection is not open then it will open it.
                    CreateResponseMessageSender(messageContext, aProtocolMessage.ResponseReceiverId);

                    Dispatcher.Invoke(() => NotifyMessageReceived(messageContext, aProtocolMessage));
                }
                else if (aProtocolMessage.MessageType == EProtocolMessageType.OpenConnectionRequest)
                {
                    // If the connection is not approved.
                    CreateResponseMessageSender(messageContext, aProtocolMessage.ResponseReceiverId);
                }
                else if (aProtocolMessage.MessageType == EProtocolMessageType.CloseConnectionRequest)
                {
                    ThreadPool.QueueUserWorkItem(x => CloseResponseMessageSender(aProtocolMessage.ResponseReceiverId, false));
                }
                else
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.ReceiveMessageIncorrectFormatFailure);
                }

                return true;
            }
        }

        private void CleanDisconnectedResponseReceiver(ISender responseSender, string senderAddress)
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
                    ThreadPool.QueueUserWorkItem(x => Dispatcher.Invoke(() => Notify(ResponseReceiverConnected, responseReceiverId, aConnectionContext.SenderAddress)));
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
                                SenderUtil.SendCloseConnection(aConnectionContext.ResponseSender, "", myProtocolFormatter);
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

        // Utility method to decode the incoming message into the ProtocolMessage.
        private ProtocolMessage GetProtocolMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                ProtocolMessage aProtocolMessage = null;

                if (message is Stream)
                {
                    aProtocolMessage = myProtocolFormatter.DecodeMessage((Stream)message);
                }
                else
                {
                    aProtocolMessage = myProtocolFormatter.DecodeMessage(message);
                }

                return aProtocolMessage;
            }
        }

        private void Notify(EventHandler<ResponseReceiverEventArgs> handler, string responseReceiverId, string senderAddress)
        {
            using (EneterTrace.Entering())
            {
                ResponseReceiverEventArgs anEventArgs = new ResponseReceiverEventArgs(responseReceiverId, senderAddress);
                Notify<ResponseReceiverEventArgs>(handler, anEventArgs, false);
            }
        }


        private void NotifyMessageReceived(MessageContext messageContext, ProtocolMessage protocolMessage)
        {
            using (EneterTrace.Entering())
            {
                DuplexChannelMessageEventArgs anEventArgs = new DuplexChannelMessageEventArgs(ChannelId, protocolMessage.Message, protocolMessage.ResponseReceiverId, messageContext.SenderAddress);
                Notify<DuplexChannelMessageEventArgs>(MessageReceived, anEventArgs, true);
            }
        }

        private void Notify<T>(EventHandler<T> handler, T eventArgs, bool isNobodySubscribedWarning)
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


        private IInputConnector myInputConnector;
        private IProtocolFormatter myProtocolFormatter;

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
