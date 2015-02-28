/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

#if !WINDOWS_PHONE_70

using System;
using System.IO;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

#if SILVERLIGHT
using System.Windows;
#endif

#if !SILVERLIGHT
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
#endif

namespace Eneter.Messaging.MessagingSystems.WebSocketMessagingSystem
{
    internal class WebSocketOutputConnector : IOutputConnector
    {
#if !SILVERLIGHT
        public WebSocketOutputConnector(string inputConnectorAddress, string outputConnectorAddress, IProtocolFormatter protocolFormatter, ISecurityFactory clientSecurityFactory,
            int connectTimeout,
            int sendTimeout,
            int receiveTimeout)
#else
        public WebSocketOutputConnector(string inputConnectorAddress,
            int connectTimeout,
            int sendTimeout,
            int receiveTimeout)
#endif
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

#if !SILVERLIGHT
                myClient = new WebSocketClient(aUri, clientSecurityFactory);
#else
                myClient = new WebSocketClient(aUri);
#endif

                myOutputConnectorAddress = outputConnectorAddress;
                myProtocolFormatter = protocolFormatter;
                myClient.ConnectTimeout = connectTimeout;
                myClient.SendTimeout = sendTimeout;
                myClient.ReceiveTimeout = receiveTimeout;
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
                        myClient.SendMessage(anEncodedMessage);
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
                lock (myConnectionManipulatorLock)
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
                lock (myConnectionManipulatorLock)
                {
                    aResponseHandler = myResponseMessageHandler;
                    CloseConnection();
                }

                ProtocolMessage aProtocolMessage = new ProtocolMessage(EProtocolMessageType.CloseConnectionRequest, myOutputConnectorAddress, null);
                MessageContext aMessageContext = new MessageContext(aProtocolMessage, myIpAddress);

                try
                {
                    // If the connection closed is not caused that the client callled CloseConnection()
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


        private string myOutputConnectorAddress;
        private IProtocolFormatter myProtocolFormatter;
        private WebSocketClient myClient;
        private Action<MessageContext> myResponseMessageHandler;
        private string myIpAddress;
        private object myConnectionManipulatorLock = new object();

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}

#endif