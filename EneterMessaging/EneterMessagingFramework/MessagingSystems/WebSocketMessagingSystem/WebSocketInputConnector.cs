/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

#if !SILVERLIGHT && !WINDOWS_PHONE_70

using System;
using System.Collections.Generic;
using System.IO;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security;

namespace Eneter.Messaging.MessagingSystems.WebSocketMessagingSystem
{
    internal class WebSocketInputConnector : IInputConnector
    {
        private class WebSocketResponseSender : ISender, IDisposable
        {
            public WebSocketResponseSender(IWebSocketClientContext clientContext)
            {
                using (EneterTrace.Entering())
                {
                    myClientContext = clientContext;
                }
            }

            public void Dispose()
            {
                using (EneterTrace.Entering())
                {
                    myClientContext.CloseConnection();
                }
            }

            public bool IsStreamWritter { get { return false; } }
            
            public void SendMessage(object message)
            {
                using (EneterTrace.Entering())
                {
                    myClientContext.SendMessage(message);
                }
            }

            public void SendMessage(Action<Stream> toStreamWritter)
            {
                throw new NotSupportedException();
            }

            private IWebSocketClientContext myClientContext;
        }

        public WebSocketInputConnector(string wsUriAddress, ISecurityFactory securityFactory, int sendTimeout, int receiveTimeout)
        {
            using (EneterTrace.Entering())
            {
                Uri aUri;
                try
                {
                    aUri = new Uri(wsUriAddress);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(wsUriAddress + ErrorHandler.InvalidUriAddress, err);
                    throw;
                }

                myListener = new WebSocketListener(aUri, securityFactory);
                mySendTimeout = sendTimeout;
                myReceiveTimeout = receiveTimeout;
            }
        }

        public void StartListening(Func<MessageContext, bool> messageHandler)
        {
            using (EneterTrace.Entering())
            {
                lock (myListenerManipulatorLock)
                {
                    try
                    {
                        myMessageHandler = messageHandler;
                        myListener.StartListening(HandleConnection);
                    }
                    catch
                    {
                        myMessageHandler = null;
                        throw;
                    }
                }
            }
        }

        public void StopListening()
        {
            using (EneterTrace.Entering())
            {
                lock (myListenerManipulatorLock)
                {
                    myListener.StopListening();
                    myMessageHandler = null;
                }
            }
        }

        public bool IsListening
        {
            get
            {
                lock (myListenerManipulatorLock)
                {
                    return myListener.IsListening;
                }
            }
        }

        public ISender CreateResponseSender(string responseReceiverAddress)
        {
            throw new NotSupportedException("CreateResponseSender is not supported in WebSocketServiceConnector.");
        }

        private void HandleConnection(IWebSocketClientContext client)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    client.SendTimeout = mySendTimeout;
                    client.ReceiveTimeout = myReceiveTimeout;

                    string aClientIp = (client.ClientEndPoint != null) ? client.ClientEndPoint.Address.ToString() : "";
                    ISender aResponseSender = new WebSocketResponseSender(client);

                    bool isConnectionOpen = true;
                    while (isConnectionOpen)
                    {
                        // Block until a message is received or the connection is closed.
                        WebSocketMessage aWebSocketMessage = client.ReceiveMessage();

                        if (aWebSocketMessage != null && myMessageHandler != null)
                        {
                            MessageContext aContext = new MessageContext(aWebSocketMessage.InputStream, aClientIp, aResponseSender);
                            if (!myMessageHandler(aContext))
                            {
                                isConnectionOpen = false;
                            }

                            if (!client.IsConnected)
                            {
                                isConnectionOpen = false;
                            }
                        }
                        else
                        {
                            isConnectionOpen = false;
                        }
                    }
                }
                finally
                {
                    client.CloseConnection();
                }
            }
        }


        private WebSocketListener myListener;
        private Func<MessageContext, bool> myMessageHandler;
        private object myListenerManipulatorLock = new object();
        private List<IWebSocketClientContext> myConnectedSenders = new List<IWebSocketClientContext>();
        private int mySendTimeout;
        private int myReceiveTimeout;
    }
}


#endif