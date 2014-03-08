/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/


using System;
using System.IO;
using System.Threading;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase
{
    internal class DefaultDuplexOutputChannel : IDuplexOutputChannel
    {
        public event EventHandler<DuplexChannelMessageEventArgs> ResponseMessageReceived;
        public event EventHandler<DuplexChannelEventArgs> ConnectionOpened;
        public event EventHandler<DuplexChannelEventArgs> ConnectionClosed;

        public DefaultDuplexOutputChannel(string channelId, string responseReceiverId, IThreadDispatcher dispatcher,
            IOutputConnectorFactory outputConnectorFactory, IProtocolFormatter protocolFormatter, bool startReceiverAfterSendOpenRequest)
        {
            using (EneterTrace.Entering())
            {
                if (string.IsNullOrEmpty(channelId))
                {
                    EneterTrace.Error(ErrorHandler.NullOrEmptyChannelId);
                    throw new ArgumentException(ErrorHandler.NullOrEmptyChannelId);
                }

                ChannelId = channelId;

                if (string.IsNullOrEmpty(responseReceiverId))
                {
                    string aResponseReceiverId = channelId.TrimEnd('/');
                    aResponseReceiverId += "_" + Guid.NewGuid().ToString() + "/";
                    ResponseReceiverId = aResponseReceiverId;
                }
                else
                {
                    ResponseReceiverId = responseReceiverId;
                }

                myProtocolFormatter = protocolFormatter;

                Dispatcher = dispatcher;
                myOutputConnector = outputConnectorFactory.CreateOutputConnector(ChannelId, ResponseReceiverId);
                myStartReceiverAfterSendOpenRequest = startReceiverAfterSendOpenRequest;
            }
        }

        public string ChannelId { get; private set; }
        public string ResponseReceiverId { get; private set; }


        public void OpenConnection()
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    if (IsConnected)
                    {
                        string aMessage = TracedObject + ErrorHandler.IsAlreadyConnected;
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    try
                    {
                        if (!myStartReceiverAfterSendOpenRequest)
                        {
                            // Connect and start listening to response messages.
                            myOutputConnector.OpenConnection(HandleResponse);
                        }

                        // Send the open connection request.
                        SenderUtil.SendOpenConnection(myOutputConnector, ResponseReceiverId, myProtocolFormatter);

                        if (myStartReceiverAfterSendOpenRequest)
                        {
                            // Connect and start listening to response messages.
                            myOutputConnector.OpenConnection(HandleResponse);
                        }

                        myConnectionIsCorrectlyOpen = true;

                        // Invoke the event notifying, the connection was opened.
                        ThreadPool.QueueUserWorkItem(x => Dispatcher.Invoke(() => Notify(ConnectionOpened)));
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.OpenConnectionFailure, err);

                        try
                        {
                            CloseConnection();
                        }
                        catch
                        {
                            // We tried to clean after failure. The exception can be ignored.
                        }

                        throw;
                    }
                }
            }
        }

        public void CloseConnection()
        {
            using (EneterTrace.Entering())
            {
                CleanAfterConnection(true);
            }
        }

        public bool IsConnected
        {
            get
            {
                lock (myConnectionManipulatorLock)
                {
                    return (myOutputConnector.IsConnected);
                }
            }
        }

        public void SendMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    if (!IsConnected)
                    {
                        string aMessage = TracedObject + ErrorHandler.SendMessageNotConnectedFailure;
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    try
                    {
                        // Send the message.
                        SenderUtil.SendMessage(myOutputConnector, ResponseReceiverId, message, myProtocolFormatter);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.SendMessageFailure, err);
                        throw;
                    }
                }
            }
        }

        public IThreadDispatcher Dispatcher { get; private set; }

        private bool HandleResponse(MessageContext messageContext)
        {
            using (EneterTrace.Entering())
            {
                ProtocolMessage aProtocolMessage = null;

                if (messageContext != null && messageContext.Message != null)
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
                else
                {
                    EneterTrace.Warning(TracedObject + "detected null response message. It means the listening to responses stopped.");
                }


                if (aProtocolMessage == null || aProtocolMessage.MessageType == EProtocolMessageType.CloseConnectionRequest)
                {
                    EneterTrace.Debug("CLIENT DISCONNECTED RECEIVED");

                    ThreadPool.QueueUserWorkItem(x => CleanAfterConnection(false));

                    // The connection is closed so stop listening to messages.
                    return false;
                }
                else if (aProtocolMessage.MessageType == EProtocolMessageType.MessageReceived)
                {
                    EneterTrace.Debug("RESPONSE MESSAGE RECEIVED");

                    Dispatcher.Invoke(() => NotifyResponseMessageReceived(aProtocolMessage.Message));
                }
                else
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.ReceiveMessageIncorrectFormatFailure);
                }


                return aProtocolMessage != null;
            }
        }

        private void CleanAfterConnection(bool sendCloseMessageFlag)
        {
            using (EneterTrace.Entering())
            {
                bool aNotifyFlag;

                lock (myConnectionManipulatorLock)
                {
                    // Try to notify that the connection is closed
                    if (sendCloseMessageFlag)
                    {
                        try
                        {
                            // Send the close connection request.
                            SenderUtil.SendCloseConnection(myOutputConnector, ResponseReceiverId, myProtocolFormatter);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.CloseConnectionFailure, err);
                        }
                    }

                    try
                    {
                        myOutputConnector.CloseConnection();
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.CloseConnectionFailure, err);
                    }

                    // Note: the notification must run outside the lock because of potententional deadlock.
                    aNotifyFlag = myConnectionIsCorrectlyOpen;
                    myConnectionIsCorrectlyOpen = false;
                }

                // Notify the connection closed only if it was successfuly open before.
                // E.g. It will be not notified if the CloseConnection() is called for already closed connection.
                if (aNotifyFlag)
                {
                    Dispatcher.Invoke(() => Notify(ConnectionClosed));
                }
            }
        }


        private void NotifyResponseMessageReceived(object message)
        {
            using (EneterTrace.Entering())
            {
                if (ResponseMessageReceived != null)
                {
                    try
                    {
                        ResponseMessageReceived(this, new DuplexChannelMessageEventArgs(ChannelId, message, ResponseReceiverId, ""));
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
                else
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.NobodySubscribedForMessage);
                }
            }
        }

        private void Notify(EventHandler<DuplexChannelEventArgs> eventHandler)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    if (eventHandler != null)
                    {
                        DuplexChannelEventArgs aMsg = new DuplexChannelEventArgs(ChannelId, ResponseReceiverId, "");
                        eventHandler(this, aMsg);
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                }
            }
        }


        private IOutputConnector myOutputConnector;
        private bool myStartReceiverAfterSendOpenRequest;
        private bool myConnectionIsCorrectlyOpen;
        private object myConnectionManipulatorLock = new object();

        private IProtocolFormatter myProtocolFormatter;

        private string TracedObject
        {
            get
            {
                return GetType().Name + " '" + ChannelId + "' ";
            }
        }
        
    }
}
