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
#endif

namespace Eneter.Messaging.MessagingSystems.WebSocketMessagingSystem
{
    internal class WebSocketOutputConnector : IOutputConnector
    {
#if !SILVERLIGHT
        public WebSocketOutputConnector(string inputConnectorAddress, ISecurityFactory clientSecurityFactory,
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

                myClient.ConnectTimeout = connectTimeout;
                myClient.SendTimeout = sendTimeout;
                myClient.ReceiveTimeout = receiveTimeout;
            }
        }

        public void OpenConnection(Func<MessageContext, bool> responseMessageHandler)
        {
            using (EneterTrace.Entering())
            {
                if (responseMessageHandler != null)
                {
                    myClient.ConnectionClosed += OnWebSocketConnectionClosed;
                    myClient.MessageReceived += OnWebSocketMessageReceived;
                    myResponseMessageHandler = responseMessageHandler;
                }

                myClient.OpenConnection();

#if !SILVERLIGHT
                myIpAddress = (myClient.LocalEndPoint != null) ? myClient.LocalEndPoint.ToString() : "";
#else
                myIpAddress = "";
#endif
            }
        }

        public void CloseConnection()
        {
            using (EneterTrace.Entering())
            {
                myClient.CloseConnection();
                myClient.MessageReceived -= OnWebSocketMessageReceived;
                myClient.ConnectionClosed -= OnWebSocketConnectionClosed;
            }
        }

        public bool IsConnected { get { return myClient.IsConnected; } }


        public bool IsStreamWritter { get { return false; } }

        public void SendMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                myClient.SendMessage(message);
            }
        }

        public void SendMessage(Action<Stream> toStreamWritter)
        {
            throw new NotSupportedException();
        }


        private void OnWebSocketConnectionClosed(object sender, EventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (myResponseMessageHandler != null)
                {
                    // With null indicate the connection was closed.
                    myResponseMessageHandler(null);
                }
            }
        }

        private void OnWebSocketMessageReceived(object sender, WebSocketMessage e)
        {
            using (EneterTrace.Entering())
            {
                if (myResponseMessageHandler != null)
                {
                    MessageContext aContext = new MessageContext(e.InputStream, myIpAddress, this);

                    try
                    {
                        myResponseMessageHandler(aContext);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }


        private WebSocketClient myClient;
        private Func<MessageContext, bool> myResponseMessageHandler;
        private string myIpAddress;

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}

#endif