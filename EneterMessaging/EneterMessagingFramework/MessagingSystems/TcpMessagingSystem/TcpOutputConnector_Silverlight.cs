/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

#if SILVERLIGHT && !WINDOWS_PHONE_70

using System;
using System.IO;
using System.Threading;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;

namespace Eneter.Messaging.MessagingSystems.TcpMessagingSystem
{
    internal class TcpOutputConnector : IOutputConnector
    {
        public TcpOutputConnector(string ipAddressAndPort, string outputConnectorAddress, IProtocolFormatter protocolFormatter,
            int connectTimeout,
            int sendTimeout,
            int receiveTimeout)
        {
            using (EneterTrace.Entering())
            {
                if (string.IsNullOrEmpty(ipAddressAndPort))
                {
                    EneterTrace.Error(ErrorHandler.NullOrEmptyChannelId);
                    throw new ArgumentException(ErrorHandler.NullOrEmptyChannelId);
                }

                try
                {
                    // just check if the address is valid
                    myUriBuilder = new UriBuilder(ipAddressAndPort);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.InvalidUriAddress, err);
                    throw;
                }

                myOutputConnectorAddress = outputConnectorAddress;
                myProtocolFormatter = protocolFormatter;
                myConnectionTimeout = connectTimeout;
                mySendTimeout = sendTimeout;
                myReceiveTimeout = receiveTimeout;
            }
        }

        public void OpenConnection(Action<MessageContext> responseMessageHandler)
        {
            using (EneterTrace.Entering())
            {
                if (responseMessageHandler == null)
                {
                    throw new ArgumentNullException("responseMessageHandler is null.");
                }

                lock (myConnectionManipulatorLock)
                {
                    try
                    {
                        // 4502 and 4532 ???
                        myTcpClient = new TcpClient(myUriBuilder.Host, myUriBuilder.Port);

                        myTcpClient.ConnectTimeout = myConnectionTimeout;
                        myTcpClient.SendTimeout = mySendTimeout;
                        myTcpClient.ReceiveTimeout = myReceiveTimeout;

                        myTcpClient.Connect();

                        myStopReceivingRequestedFlag = false;
                        myResponseMessageHandler = responseMessageHandler;

                        // Thread responsible for reading meaningfull message from the DynamicStream.
                        // It recognizes messages from RAW bytes in the stream. If the message is
                        // not complete it waits for missing data.
                        myResponseReceiverThread = new Thread(DoResponseListening);
                        myResponseReceiverThread.Start();

                        // Try to wait until the thread listening to responses started.
                        myListeningToResponsesStartedEvent.WaitOne(1000);

                        byte[] anEncodedMessage = (byte[])myProtocolFormatter.EncodeOpenConnectionMessage(myOutputConnectorAddress);
                        if (anEncodedMessage != null)
                        {
                            myTcpClient.Send(anEncodedMessage);
                        }
                    }
                    catch
                    {
                        CloseConnection();
                        throw;
                    }
                }
            }
        }

        public void CloseConnection()
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    myStopReceivingRequestedFlag = true;

                    if (myTcpClient != null)
                    {
                        myTcpClient.Close();
                        myTcpClient = null;
                    }

                    if (myResponseReceiverThread != null && myResponseReceiverThread.ThreadState != ThreadState.Unstarted)
                    {
                        if (!myResponseReceiverThread.Join(3000))
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.FailedToStopThreadId + myResponseReceiverThread.ManagedThreadId);

                            try
                            {
                                myResponseReceiverThread.Abort();
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.FailedToAbortThread, err);
                            }
                        }
                    }

                    myResponseReceiverThread = null;
                    myResponseMessageHandler = null;
                }
            }
        }

        public bool IsConnected
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    lock (myConnectionManipulatorLock)
                    {
                        return myTcpClient != null && myIsListeningToResponses;
                    }
                }
            }
        }


        public void SendRequestMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    byte[] anEncodedMessage = (byte[])myProtocolFormatter.EncodeMessage(myOutputConnectorAddress, message);
                    myTcpClient.Send(anEncodedMessage);
                }
            }
        }

        private void DoResponseListening()
        {
            using (EneterTrace.Entering())
            {
                myIsListeningToResponses = true;
                myListeningToResponsesStartedEvent.Set();

                try
                {
                    Stream anInputStream = myTcpClient.GetInputStream();

                    while (!myStopReceivingRequestedFlag)
                    {
                        ProtocolMessage aProtocolMessage = myProtocolFormatter.DecodeMessage((Stream)anInputStream);
                        if (aProtocolMessage == null)
                        {
                            // The client is disconneced by the service.
                            break;
                        }

                        MessageContext aMessageContext = new MessageContext(aProtocolMessage, "");

                        try
                        {
                            myResponseMessageHandler(aMessageContext);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                        }
                    }
                }
                catch (Exception err)
                {
                    // If it is not an exception caused by closing the socket.
                    if (!myStopReceivingRequestedFlag)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.FailedInListeningLoop, err);
                    }
                }

                myIsListeningToResponses = false;
                myListeningToResponsesStartedEvent.Reset();

                // If this closing is not caused by CloseConnection method.
                if (!myStopReceivingRequestedFlag)
                {
                    Action<MessageContext> aResponseHandler = myResponseMessageHandler;
                    CloseConnection();

                    try
                    {
                        aResponseHandler(null);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.FailedInListeningLoop, err);
                    }
                }
            }
        }


        private UriBuilder myUriBuilder;
        private string myOutputConnectorAddress;
        private TcpClient myTcpClient;
        private IProtocolFormatter myProtocolFormatter;
        private int myConnectionTimeout;
        private int mySendTimeout;
        private int myReceiveTimeout;
        private object myConnectionManipulatorLock = new object();

        private Action<MessageContext> myResponseMessageHandler;
        private Thread myResponseReceiverThread;
        private volatile bool myStopReceivingRequestedFlag;
        private volatile bool myIsListeningToResponses;
        private ManualResetEvent myListeningToResponsesStartedEvent = new ManualResetEvent(false);

        private string TracedObject { get { return GetType().Name + " "; } }
    }
}

#endif