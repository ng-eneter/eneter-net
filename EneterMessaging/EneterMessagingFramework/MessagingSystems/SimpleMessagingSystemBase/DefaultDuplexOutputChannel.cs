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

        public DefaultDuplexOutputChannel(string channelId, string responseReceiverId,
            IThreadDispatcher eventDispatcher,
            IThreadDispatcher dispatcherAfterResponseReading,
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

                Dispatcher = eventDispatcher;

                // Internal dispatcher used when the message is decoded.
                // E.g. Shared memory meesaging needs to return looping immediately the protocol message is decoded
                //      so that other senders are not blocked.
                myDispatchingAfterResponseReading = dispatcherAfterResponseReading;

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
                        myConnectionIsCorrectlyOpen = true;

                        // If first the connection is open and then 'OpenConneciton' message is sent.
                        // E.g. in case of TCP
                        if (!myStartReceiverAfterSendOpenRequest)
                        {
                            // Connect and start listening to response messages.
                            myOutputConnector.OpenConnection(HandleResponse);
                        }

                        // Send the open connection request.
                        SenderUtil.SendOpenConnection(myOutputConnector, ResponseReceiverId, myProtocolFormatter);

                        // If first 'OpenConnection' request message is sent and the the connection can be open.
                        // E.g. in case of Shared Memory messaging DuplexOutputChannel sends 'OpenConnecion' message
                        //      to DuplexInputChannel. When DuplexInputChannel receives the message it creates
                        //      shared memory for sending response messages to DuplexOutputChannel.
                        //      Only then DuplexOutputChannel can open connection.
                        //      Because only then the shared memory for response messages is created.
                        if (myStartReceiverAfterSendOpenRequest)
                        {
                            // Connect and start listening to response messages.
                            myOutputConnector.OpenConnection(HandleResponse);
                        }
                    }
                    catch (Exception err)
                    {
                        myConnectionIsCorrectlyOpen = false;

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

                // Invoke the event notifying, the connection was opened.
                Dispatcher.Invoke(() => Notify(ConnectionOpened));
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
                    myDispatchingAfterResponseReading.Invoke(() => CleanAfterConnection(false));
                    return false;
                }
                else if (aProtocolMessage.MessageType == EProtocolMessageType.MessageReceived)
                {
                    EneterTrace.Debug("RESPONSE MESSAGE RECEIVED");
                    myDispatchingAfterResponseReading.Invoke(() => Dispatcher.Invoke(() => NotifyResponseMessageReceived(aProtocolMessage.Message)));
                }
                else
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.ReceiveMessageIncorrectFormatFailure);
                }


                return true;
            }
        }

        private void CleanAfterConnection(bool sendCloseMessageFlag)
        {
            using (EneterTrace.Entering())
            {
                bool aNotifyFlag = false;

                // If close connection is already being performed then do not close again.
                // E.g. CloseConnection() and this will stop a response listening thread. This thread would come here and
                //      would stop on the following lock -> deadlock.
                if (!myCloseConnectionIsRunning)
                {
                    lock (myConnectionManipulatorLock)
                    {
                        myCloseConnectionIsRunning = true;

                        // Try to notify that the connection is closed
                        // Note: if it is that first the connection is created and then 'OpenConnecion' message is sent (e.g. TCP)
                        //       then try to send 'CloseConnection' message.
                        //       But if it is that first 'OpenConnection' request message is sent and only then the connection is open (e.g. Shared Memory)
                        //       then do not send 'CloseConnection' message is the connection is not open.
                        //       It would just cause timeoute to realize the message cannot be sent.
                        if (sendCloseMessageFlag && !myStartReceiverAfterSendOpenRequest ||
                            sendCloseMessageFlag && myStartReceiverAfterSendOpenRequest && myOutputConnector.IsConnected)
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
                        myCloseConnectionIsRunning = false;
                    }
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


        private IThreadDispatcher myDispatchingAfterResponseReading;
        private IOutputConnector myOutputConnector;
        private bool myStartReceiverAfterSendOpenRequest;
        private bool myConnectionIsCorrectlyOpen;
        private bool myCloseConnectionIsRunning;
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
