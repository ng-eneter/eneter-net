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
            Authenticate verifyHandshakeResponseMessageCallback)
        {
            using (EneterTrace.Entering())
            {
                myUnderlayingInputChannel = underlyingInputChannel;
                myGetHandshakeMessageCallback = getHandshakeMessageCallback;
                myAuthenticateCallback = verifyHandshakeResponseMessageCallback;

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
                lock (myAuthenticatedConnections)
                {
                    if (!myAuthenticatedConnections.Contains(responseReceiverId))
                    {
                        string aMessage = TracedObject + ErrorHandler.SendResponseNotConnectedFailure;
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    myUnderlayingInputChannel.SendResponseMessage(responseReceiverId, message);
                }
            }
        }

        public void DisconnectResponseReceiver(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                myUnderlayingInputChannel.DisconnectResponseReceiver(responseReceiverId);
            }
        }

        private void OnResponseReceiverDisconnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                bool anIsAuthenticatedConnection;
                lock (myAuthenticatedConnections)
                {
                    anIsAuthenticatedConnection = myAuthenticatedConnections.Remove(e.ResponseReceiverId);
                }

                lock (myNotYetAuthenticatedConnections)
                {
                    myNotYetAuthenticatedConnections.Remove(e.ResponseReceiverId);
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
                lock (myAuthenticatedConnections)
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

                lock (myNotYetAuthenticatedConnections)
                {
                    TNotYetAuthenticatedConnection aConnection;
                    myNotYetAuthenticatedConnections.TryGetValue(e.ResponseReceiverId, out aConnection);
                    if (aConnection == null)
                    {
                        aConnection = new TNotYetAuthenticatedConnection();
                        myNotYetAuthenticatedConnections[e.ResponseReceiverId] = aConnection;
                    }

                    // If the connection is in the state that the handshake message was sent then this is the handshake response message.
                    // The response for the handshake will be verified.
                    if (aConnection.HandshakeMessage != null)
                    {
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

                                    // Move the connection among authenticated connections.
                                    myNotYetAuthenticatedConnections.Remove(e.ResponseReceiverId);
                                    lock (myAuthenticatedConnections)
                                    {
                                        myAuthenticatedConnections.Add(e.ResponseReceiverId);
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
                    else
                    {
                        // If the connection is in the state that it is not logged in then this must be the login message.
                        // The handshake message will be sent.
                        try
                        {
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

                    if (aDisconnectFlag)
                    {
                        myNotYetAuthenticatedConnections.Remove(e.ResponseReceiverId);
                    }
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

        private string TracedObject
        {
            get
            {
                return GetType().Name + " '" + ChannelId + "' ";
            }
        }
    }
}
