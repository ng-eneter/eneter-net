/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2014
*/

using System;
using System.Collections.Generic;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.MessagingSystems.Composites.AuthenticatedConnection
{
    internal class AuthenticatedDuplexInputChannel : IDuplexInputChannel
    {
        private class TNotYetAuthenticatedConnection
        {
            public object LoginMessage { get; set; }
            public object HandshakeMessage { get; set; }
        }

        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;
        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;
        public event EventHandler<DuplexChannelMessageEventArgs> MessageReceived;


        public AuthenticatedDuplexInputChannel(IDuplexInputChannel underlyingInputChannel,
            GetHanshakeMessage getHandshakeMessageCallback,
            Authenticate verifyHandshakeResponseMessageCallback,
            HandleAuthenticationCancelled authenticationCancelledCallback)
        {
            using (EneterTrace.Entering())
            {
                myUnderlayingInputChannel = underlyingInputChannel;
                myGetHandshakeMessageCallback = getHandshakeMessageCallback;
                myAuthenticateCallback = verifyHandshakeResponseMessageCallback;
                myAuthenticationCancelledCallback = authenticationCancelledCallback;

                myUnderlayingInputChannel.ResponseReceiverDisconnected += OnResponseReceiverDisconnected;
                myUnderlayingInputChannel.MessageReceived += OnMessageReceived;
            }
        }

       
        public string ChannelId { get { return myUnderlayingInputChannel.ChannelId; } }
        

        public void StartListening()
        {
            using (EneterTrace.Entering())
            {
                myUnderlayingInputChannel.StartListening();
            }
        }

        public void StopListening()
        {
            using (EneterTrace.Entering())
            {
                myUnderlayingInputChannel.StopListening();
            }
        }

        public bool IsListening { get { return myUnderlayingInputChannel.IsListening; } }

        public IThreadDispatcher Dispatcher { get { return myUnderlayingInputChannel.Dispatcher; } }
        

        public void SendResponseMessage(string responseReceiverId, object message)
        {
            using (EneterTrace.Entering())
            {
                if (!IsListening)
                {
                    string aMessage = TracedObject + ErrorHandler.FailedToSendResponseBecauseNotListening;
                    EneterTrace.Error(aMessage);
                    throw new InvalidOperationException(aMessage);
                }

                if (responseReceiverId == "*")
                {
                    using (ThreadLock.Lock(myAuthenticatedConnections))
                    {
                        // Send the response message to all connected clients.
                        foreach (string aConnectedClient in myAuthenticatedConnections)
                        {
                            try
                            {
                                // Send the response message.
                                myUnderlayingInputChannel.SendResponseMessage(aConnectedClient, message);
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Error(TracedObject + ErrorHandler.FailedToSendResponseMessage, err);

                                // Note: Exception is not rethrown because if sending to one client fails it should not
                                //       affect sending to other clients.
                            }
                        }
                    }
                }
                else
                {
                    using (ThreadLock.Lock(myAuthenticatedConnections))
                    {
                        if (!myAuthenticatedConnections.Contains(responseReceiverId))
                        {
                            string aMessage = TracedObject + ErrorHandler.FailedToSendResponseBecauaeClientNotConnected;
                            EneterTrace.Error(aMessage);
                            throw new InvalidOperationException(aMessage);
                        }
                    }

                    myUnderlayingInputChannel.SendResponseMessage(responseReceiverId, message);
                }
            }
        }

        public void DisconnectResponseReceiver(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myAuthenticatedConnections))
                {
                    myAuthenticatedConnections.Remove(responseReceiverId);
                }

                using (ThreadLock.Lock(myNotYetAuthenticatedConnections))
                {
                    myNotYetAuthenticatedConnections.Remove(responseReceiverId);
                }

                myUnderlayingInputChannel.DisconnectResponseReceiver(responseReceiverId);
            }
        }

        private void OnResponseReceiverDisconnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                TNotYetAuthenticatedConnection aNotAuthenticatedConnection;
                using (ThreadLock.Lock(myNotYetAuthenticatedConnections))
                {
                    myNotYetAuthenticatedConnections.TryGetValue(e.ResponseReceiverId, out aNotAuthenticatedConnection);
                    if (aNotAuthenticatedConnection != null)
                    {
                        myNotYetAuthenticatedConnections.Remove(e.ResponseReceiverId);
                    }
                }
                if (aNotAuthenticatedConnection != null && myAuthenticationCancelledCallback != null)
                {
                    try
                    {
                        myAuthenticationCancelledCallback(ChannelId, e.ResponseReceiverId, aNotAuthenticatedConnection.LoginMessage);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }

                bool anIsAuthenticatedConnection;
                using (ThreadLock.Lock(myAuthenticatedConnections))
                {
                    anIsAuthenticatedConnection = myAuthenticatedConnections.Remove(e.ResponseReceiverId);
                }
                if (anIsAuthenticatedConnection)
                {
                    Notify<ResponseReceiverEventArgs>(ResponseReceiverDisconnected, e, false);
                }
            }
        }

        private void OnMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                // If the connection has already been authenticated then this is a regular request message that will be notified.
                bool anIsAuthenticated;
                using (ThreadLock.Lock(myAuthenticatedConnections))
                {
                    anIsAuthenticated = myAuthenticatedConnections.Contains(e.ResponseReceiverId);
                }
                if (anIsAuthenticated)
                {
                    Notify<DuplexChannelMessageEventArgs>(MessageReceived, e, true);
                    return;
                }


                bool aDisconnectFlag = true;
                bool aNewResponseReceiverAuthenticated = false;

                TNotYetAuthenticatedConnection aConnection;
                using (ThreadLock.Lock(myNotYetAuthenticatedConnections))
                {
                    myNotYetAuthenticatedConnections.TryGetValue(e.ResponseReceiverId, out aConnection);
                    if (aConnection == null)
                    {
                        aConnection = new TNotYetAuthenticatedConnection();
                        myNotYetAuthenticatedConnections[e.ResponseReceiverId] = aConnection;
                    }
                }

                if (aConnection.HandshakeMessage == null)
                {
                    EneterTrace.Debug("LOGIN RECEIVED");

                    // If the connection is in the state that it is not logged in then this must be the login message.
                    // The handshake message will be sent.
                    try
                    {
                        aConnection.LoginMessage = e.Message;
                        aConnection.HandshakeMessage = myGetHandshakeMessageCallback(e.ChannelId, e.ResponseReceiverId, e.Message);

                        // If the login was accepted.
                        if (aConnection.HandshakeMessage != null)
                        {
                            try
                            {
                                // Send the handshake message to the client.
                                myUnderlayingInputChannel.SendResponseMessage(e.ResponseReceiverId, aConnection.HandshakeMessage);
                                aDisconnectFlag = false;
                            }
                            catch (Exception err)
                            {
                                string anErrorMessage = TracedObject + "failed to send the handshake message. The client will be disconnected.";
                                EneterTrace.Error(anErrorMessage, err);
                            }
                        }
                        else
                        {
                            // the client will be disconnected.
                        }
                    }
                    catch (Exception err)
                    {
                        string anErrorMessage = TracedObject + "failed to get the handshake message. The client will be disconnected.";
                        EneterTrace.Error(anErrorMessage, err);
                    }
                }
                // If the connection is in the state that the handshake message was sent then this is the handshake response message.
                // The response for the handshake will be verified.
                else
                {
                    EneterTrace.Debug("HANDSHAKE RESPONSE RECEIVED");

                    try
                    {
                        if (myAuthenticateCallback(e.ChannelId, e.ResponseReceiverId, aConnection.LoginMessage, aConnection.HandshakeMessage, e.Message))
                        {
                            // Send acknowledge message that the connection is authenticated.
                            try
                            {
                                myUnderlayingInputChannel.SendResponseMessage(e.ResponseReceiverId, "OK");

                                aDisconnectFlag = false;
                                aNewResponseReceiverAuthenticated = true;

                                // Move the connection to authenticated connections.
                                using (ThreadLock.Lock(myAuthenticatedConnections))
                                {
                                    myAuthenticatedConnections.Add(e.ResponseReceiverId);
                                }
                                using (ThreadLock.Lock(myNotYetAuthenticatedConnections))
                                {
                                    myNotYetAuthenticatedConnections.Remove(e.ResponseReceiverId);
                                }
                            }
                            catch (Exception err)
                            {
                                string anErrorMessage = TracedObject + "failed to send the acknowledge message that the connection was authenticated. The client will be disconnected.";
                                EneterTrace.Error(anErrorMessage, err);
                            }
                        }
                    }
                    catch (Exception err)
                    {
                        string anErrorMessage = TracedObject + "failed to verify the response for the handshake message. The client will be disconnected.";
                        EneterTrace.Error(anErrorMessage, err);
                    }
                }
                    

                if (aDisconnectFlag)
                {
                    myNotYetAuthenticatedConnections.Remove(e.ResponseReceiverId);
                }

                // If the connection with the client shall be closed.
                // Note: the disconnection runs outside the lock in order to reduce blocking.
                if (aDisconnectFlag)
                {
                    try
                    {
                        myUnderlayingInputChannel.DisconnectResponseReceiver(e.ResponseReceiverId);
                    }
                    catch (Exception err)
                    {
                        string anErrorMessage = TracedObject + "failed to disconnect response receiver.";
                        EneterTrace.Warning(anErrorMessage, err);
                    }
                }

                // Notify ResponseReceiverConnected if a new connection is authenticated.
                // Note: the notification runs outside the lock in order to reduce blocking.
                if (aNewResponseReceiverAuthenticated)
                {
                    ResponseReceiverEventArgs anEventArgs = new ResponseReceiverEventArgs(e.ResponseReceiverId, e.SenderAddress);
                    Notify<ResponseReceiverEventArgs>(ResponseReceiverConnected, anEventArgs, false);
                }

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

        private IDuplexInputChannel myUnderlayingInputChannel;

        private Dictionary<string, TNotYetAuthenticatedConnection> myNotYetAuthenticatedConnections = new Dictionary<string, TNotYetAuthenticatedConnection>();
        private HashSet<string> myAuthenticatedConnections = new HashSet<string>();

        private GetHanshakeMessage myGetHandshakeMessageCallback;
        private Authenticate myAuthenticateCallback;
        private HandleAuthenticationCancelled myAuthenticationCancelledCallback;

        private string TracedObject
        {
            get
            {
                return GetType().Name + " '" + ChannelId + "' ";
            }
        }
    }
}
