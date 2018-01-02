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
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security;

#if SILVERLIGHT
using System.Windows;
#endif


namespace Eneter.Messaging.MessagingSystems.WebSocketMessagingSystem
{
    internal class WebSocketOutputConnector : IOutputConnector
    {
        public WebSocketOutputConnector(string inputConnectorAddress, string outputConnectorAddress, IProtocolFormatter protocolFormatter, ISecurityFactory clientSecurityFactory,
            int connectTimeout,
            int sendTimeout,
            int receiveTimeout,
            int pingFrequency,
            int responseReceivingPort)
        {
            using (EneterTrace.Entering())
            {
                Uri aUri;
                try
                {
                    aUri = new Uri(inputConnectorAddress, UriKind.Absolute);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(inputConnectorAddress + ErrorHandler.InvalidUriAddress, err);
                    throw;
                }

                myClient = new WebSocketClient(aUri, clientSecurityFactory);
                myClient.ResponseReceivingPort = responseReceivingPort;

                myOutputConnectorAddress = outputConnectorAddress;
                myProtocolFormatter = protocolFormatter;
                myClient.ConnectTimeout = connectTimeout;
                myClient.SendTimeout = sendTimeout;
                myClient.ReceiveTimeout = receiveTimeout;

                myPingFrequency = pingFrequency;
                myTimer = new Timer(OnPing, null, -1, -1);
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

                using (ThreadLock.Lock(myConnectionManipulatorLock))
                {
                    try
                    {
                        myResponseMessageHandler = responseMessageHandler;
                        myClient.ConnectionClosed += OnWebSocketConnectionClosed;
                        myClient.MessageReceived += OnWebSocketMessageReceived;

                        myClient.OpenConnection();

#if !SILVERLIGHT
                        myIpAddress = (myClient.LocalEndPoint != null) ? myClient.LocalEndPoint.ToString() : "";
#else
                        myIpAddress = "";
#endif

                        object anEncodedMessage = myProtocolFormatter.EncodeOpenConnectionMessage(myOutputConnectorAddress);
                        if (anEncodedMessage != null)
                        {
                            myClient.SendMessage(anEncodedMessage);
                        }

                        myTimer.Change(myPingFrequency, -1);
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
                using (ThreadLock.Lock(myConnectionManipulatorLock))
                {
                    // Note: do not send a close message in WebSockets. Just close the socket.

                    // Note: this must be before myClient.CloseConnection().
                    myResponseMessageHandler = null;

                    myClient.CloseConnection();
                    myClient.MessageReceived -= OnWebSocketMessageReceived;
                    myClient.ConnectionClosed -= OnWebSocketConnectionClosed;
                }
            }
        }

        public bool IsConnected { get { return myClient.IsConnected; } }

        public void SendRequestMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myConnectionManipulatorLock))
                {
                    object anEncodedMessage = myProtocolFormatter.EncodeMessage(myOutputConnectorAddress, message);
                    myClient.SendMessage(anEncodedMessage);
                }
            }
        }

        private void OnWebSocketConnectionClosed(object sender, EventArgs e)
        {
            using (EneterTrace.Entering())
            {
                Action<MessageContext> aResponseHandler;
                using (ThreadLock.Lock(myConnectionManipulatorLock))
                {
                    aResponseHandler = myResponseMessageHandler;
                    CloseConnection();
                }

                ProtocolMessage aProtocolMessage = new ProtocolMessage(EProtocolMessageType.CloseConnectionRequest, myOutputConnectorAddress, null);
                MessageContext aMessageContext = new MessageContext(aProtocolMessage, myIpAddress);

                try
                {
                    // If the connection closed is not caused that the client called CloseConnection()
                    // but the connection was closed from the service.
                    if (aResponseHandler != null)
                    {
                        aResponseHandler(aMessageContext);
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                }
            }
        }

        private void OnWebSocketMessageReceived(object sender, WebSocketMessage e)
        {
            using (EneterTrace.Entering())
            {
                ProtocolMessage aProtocolMessage = myProtocolFormatter.DecodeMessage((Stream)e.InputStream);
                MessageContext aMessageContext = new MessageContext(aProtocolMessage, myIpAddress);

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

        private void OnPing(Object o)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    myClient.SendPing();
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + "failed to send the ping.", err);
                }

                // If the connection is open then schedule the next ping.
                if (IsConnected)
                {
                    myTimer.Change(myPingFrequency, -1);
                }
            }
        }


        private string myOutputConnectorAddress;
        private IProtocolFormatter myProtocolFormatter;
        private WebSocketClient myClient;
        private Action<MessageContext> myResponseMessageHandler;
        private string myIpAddress;
        private object myConnectionManipulatorLock = new object();
        private Timer myTimer;
        private int myPingFrequency;

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}