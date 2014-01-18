using System;
using System.Collections.Generic;
using System.Linq;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.MessagingSystems.Composites.AuthenticatedConnection
{
    internal class AuthenticatedDuplexInputChannel : IDuplexInputChannel
    {
        private class NotYetAuthenticatedConnection
        {
            public NotYetAuthenticatedConnection(string responseReceiverId)
            {
                ResponseReceiverId = responseReceiverId;
                HandshakeMessage = Guid.NewGuid().ToString();
            }

            public string ResponseReceiverId { get; private set; }
            public string HandshakeMessage { get; private set; }
        }

        public event EventHandler<ConnectionTokenEventArgs> ResponseReceiverConnecting;
        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;
        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;
        public event EventHandler<DuplexChannelMessageEventArgs> MessageReceived;


        public AuthenticatedDuplexInputChannel(IDuplexInputChannel underlyingInputChannel, AuthenticateCallback authenticationCallback)
        {
            using (EneterTrace.Entering())
            {
                myUnderlayingInputChannel = underlyingInputChannel;
                myVerify = authenticationCallback;

                myUnderlayingInputChannel.ResponseReceiverConnecting += OnResponseReceiverConnecting;
                myUnderlayingInputChannel.ResponseReceiverConnected += OnResponseReceiverConnected;
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


        private void OnResponseReceiverConnecting(object sender, ConnectionTokenEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                Notify<ConnectionTokenEventArgs>(ResponseReceiverConnecting, e, false);
            }
        }


        private void OnResponseReceiverConnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                lock (myNotYetAuthenticatedConnections)
                {
                    // Create not authenticated client.
                    if (!myNotYetAuthenticatedConnections.Any(x => x.ResponseReceiverId == e.ResponseReceiverId))
                    {
                        NotYetAuthenticatedConnection aNewConnection = new NotYetAuthenticatedConnection(e.ResponseReceiverId);
                        myNotYetAuthenticatedConnections.Add(aNewConnection);

                        // Try to send the handshake message.
                        // The response for the handshake will be received in 'OnMessageReceived' method.
                        try
                        {
                            myUnderlayingInputChannel.SendResponseMessage(e.ResponseReceiverId, aNewConnection.HandshakeMessage);
                        }
                        catch (Exception err)
                        {
                            myNotYetAuthenticatedConnections.Remove(aNewConnection);

                            string anErrorMessage = TracedObject + "failed to send the handshake message.";
                            EneterTrace.Error(anErrorMessage, err);
                        }
                    }
                }
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
                    myNotYetAuthenticatedConnections.RemoveWhere(x => x.ResponseReceiverId == e.ResponseReceiverId);
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
                
                bool anIsAuthenticated;
                lock (myAuthenticatedConnections)
                {
                    anIsAuthenticated = myAuthenticatedConnections.Contains(e.ResponseReceiverId);
                }

                // If the connection has already been authenticated then just forward the message.
                if (anIsAuthenticated)
                {
                    Notify<DuplexChannelMessageEventArgs>(MessageReceived, e, true);
                }
                else
                {
                    // If the handshake verifycation is not good then disconnect the client.
                    bool aDisconnectFlag = true;

                    // If the connection is not authenticated this message must be a response for the handshake message.
                    lock (myNotYetAuthenticatedConnections)
                    {
                        NotYetAuthenticatedConnection aNotAuthenticatedConnection = myNotYetAuthenticatedConnections.FirstOrDefault(x => x.ResponseReceiverId == e.ResponseReceiverId);
                        if (aNotAuthenticatedConnection != null)
                        {
                            try
                            {
                                // Verify the client and its handshake response message.
                                if (myVerify(aNotAuthenticatedConnection.ResponseReceiverId, aNotAuthenticatedConnection.HandshakeMessage, e.Message))
                                {
                                    try
                                    {
                                        // Send back the acknowledge message.
                                        myUnderlayingInputChannel.SendResponseMessage(e.ResponseReceiverId, "OK");

                                        // The response receiver does not have to be disconnected.
                                        aDisconnectFlag = false;
                                    }
                                    catch (Exception err)
                                    {
                                        string anErrorMessage = TracedObject + "detected an exception when sending the connection acknowledge message. The client will be disconnected.";
                                        EneterTrace.Error(anErrorMessage, err);
                                    }
                                }
                            }
                            catch (Exception err)
                            {
                                string anErrorMessage = TracedObject + "detected an exception when verifying the response for the handshake message. The client will be diconnected.";
                                EneterTrace.Error(anErrorMessage, err);
                            }

                            // If the authentication was not successful then disconnect the client.
                            if (aDisconnectFlag)
                            {
                                myUnderlayingInputChannel.DisconnectResponseReceiver(e.ResponseReceiverId);
                            }
                            else
                            {
                                // Note: this lock is inside 'myNotYetAuthenticatedConnections' lock.
                                lock (myAuthenticatedConnections)
                                {
                                    myAuthenticatedConnections.Add(e.ResponseReceiverId);
                                }
                            }

                            // Remove response receiver from the group of not authenticated connections.
                            myNotYetAuthenticatedConnections.Remove(aNotAuthenticatedConnection);
                        }
                    }

                    // Note: the notification is out of the lock in order not to block it.
                    if (!aDisconnectFlag)
                    {
                        ResponseReceiverEventArgs anEventArgs = new ResponseReceiverEventArgs(e.ResponseReceiverId, e.SenderAddress);
                        Notify<ResponseReceiverEventArgs>(ResponseReceiverConnected, anEventArgs, false);
                    }
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
        private HashSet<NotYetAuthenticatedConnection> myNotYetAuthenticatedConnections = new HashSet<NotYetAuthenticatedConnection>();
        private HashSet<string> myAuthenticatedConnections = new HashSet<string>();

        private AuthenticateCallback myVerify;

        private string TracedObject
        {
            get
            {
                return GetType().Name + " '" + ChannelId + "' ";
            }
        }
    }
}
