

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.HttpMessagingSystem
{
    internal class HttpInputConnector : IInputConnector
    {
        private class HttpResponseSender : IDisposable
        {
            public HttpResponseSender(string responseReceiverId, string clientIp)
            {
                ResponseReceiverId = responseReceiverId;
                ClientIp = clientIp;
                LastPollingActivityTime = DateTime.Now;
            }

            // Note: This dispose is called when the duplex input channel disconnected the client.
            //       However, in HTTP messaging the client gets messages using the polling.
            //       So, although the service disconnected the client there can be still messages
            //       in the queue waiting for the polling.
            //       Therefore these messages must be still available after the dispose.
            public void Dispose()
            {
                using (EneterTrace.Entering())
                {
                    IsDisposed = true;
                }
            }

            public void SendResponseMessage(object message)
            {
                using (EneterTrace.Entering())
                {
                    if (IsDisposed)
                    {
                        throw new ObjectDisposedException(GetType().Name);
                    }

                    using (ThreadLock.Lock(myMessages))
                    {
                        byte[] aMessage = (byte[])message;
                        myMessages.Enqueue(aMessage);
                    }
                }
            }

            // Note: this method must be available after the dispose.
            public byte[] DequeueCollectedMessages()
            {
                using (EneterTrace.Entering())
                {
                    byte[] aDequedMessages = null;

                    using (ThreadLock.Lock(myMessages))
                    {
                        // Update the polling time.
                        LastPollingActivityTime = DateTime.Now;

                        // If there are stored messages for the receiver
                        if (myMessages.Count > 0)
                        {
                            using (MemoryStream aStreamedResponses = new MemoryStream())
                            {
                                // Dequeue responses to be sent to the response receiver.
                                // Note: Try not to exceed 1MB - better do more small transfers
                                while (myMessages.Count > 0 && aStreamedResponses.Length < 1048576)
                                {
                                    // Get the response message formatted according to the connection protocol.
                                    byte[] aResponseMessage = myMessages.Dequeue();
                                    aStreamedResponses.Write(aResponseMessage, 0, aResponseMessage.Length);
                                }

                                aDequedMessages = aStreamedResponses.ToArray();
                            }
                        }
                    }

                    return aDequedMessages;
                }
            }

            public string ResponseReceiverId { get; private set; }

            public string ClientIp { get; private set; }

            public DateTime LastPollingActivityTime { get; private set; }
            public bool IsDisposed { get; private set; }
            
            private Queue<byte[]> myMessages = new Queue<byte[]>();
        }

        public HttpInputConnector(string httpAddress, IProtocolFormatter protocolFormatter, int responseReceiverInactivityTimeout)
        {
            using (EneterTrace.Entering())
            {
                Uri aUri;
                try
                {
                    // just check if the channel id is a valid Uri
                    aUri = new Uri(httpAddress);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(httpAddress + ErrorHandler.InvalidUriAddress, err);
                    throw;
                }
                myHttpListenerProvider = new HttpWebServer(aUri.AbsoluteUri);

                myProtocolFormatter = protocolFormatter;
                myResponseReceiverInactivityTimeout = responseReceiverInactivityTimeout;


                // Initialize the timer to regularly check the timeout for connections with duplex output channels.
                // If the duplex output channel did not poll within the timeout then the connection
                // is closed and removed from the list.
                // Note: The timer is set here but not executed.
                myResponseReceiverInactivityTimer = new Timer(OnConnectionCheckTimer, null, -1, -1);
            }
        }

        public void StartListening(Action<MessageContext> messageHandler)
        {
            using (EneterTrace.Entering())
            {
                if (messageHandler == null)
                {
                    throw new ArgumentNullException("messageHandler is null.");
                }

                using (ThreadLock.Lock(myListeningManipulatorLock))
                {
                    try
                    {
                        myMessageHandler = messageHandler;
                        myHttpListenerProvider.StartListening(HandleConnection);
                    }
                    catch
                    {
                        StopListening();
                        throw;
                    }
                }
            }
        }

        public void StopListening()
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myConnectedClients))
                {
                    foreach (HttpResponseSender aClientContext in myConnectedClients)
                    {
                        try
                        {
                            CloseConnection(aClientContext);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.FailedToCloseConnection, err);
                        }
                    }

                    myConnectedClients.Clear();
                }

                using (ThreadLock.Lock(myListeningManipulatorLock))
                {
                    myHttpListenerProvider.StopListening();
                    myMessageHandler = null;
                }
            }
        }

        public bool IsListening
        {
            get
            {
                using (ThreadLock.Lock(myListeningManipulatorLock))
                {
                    return myHttpListenerProvider.IsListening;
                }
            }
        }

        public void SendResponseMessage(string outputConnectorAddress, object message)
        {
            using (EneterTrace.Entering())
            {
                HttpResponseSender aClientContext;
                using (ThreadLock.Lock(myConnectedClients))
                {
                    aClientContext = myConnectedClients.FirstOrDefault(x => x.ResponseReceiverId == outputConnectorAddress);
                }

                if (aClientContext == null)
                {
                    throw new InvalidOperationException("The connection with client '" + outputConnectorAddress + "' is not open.");
                }

                object anEncodedMessage = myProtocolFormatter.EncodeMessage(outputConnectorAddress, message);
                aClientContext.SendResponseMessage(anEncodedMessage);
            }
        }

        public void SendBroadcast(object message)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myConnectedClients))
                {
                    // Send the response message to all connected clients.
                    foreach (HttpResponseSender aClientContext in myConnectedClients)
                    {
                        if (!aClientContext.IsDisposed)
                        {
                            try
                            {
                                object anEncodedMessage = myProtocolFormatter.EncodeMessage(aClientContext.ResponseReceiverId, message);
                                aClientContext.SendResponseMessage(anEncodedMessage);
                            }
                            catch (Exception err)
                            {
                                // This should never happen because it is not called when disposed.
                                EneterTrace.Error(TracedObject + "failed to send the broadcast message.", err);
                            }
                        }
                    }
                }
            }
        }

        public void CloseConnection(string outputConnectorAddress)
        {
            using (EneterTrace.Entering())
            {
                HttpResponseSender aClientContext;
                using (ThreadLock.Lock(myConnectedClients))
                {
                    aClientContext = myConnectedClients.FirstOrDefault(x => x.ResponseReceiverId == outputConnectorAddress);

                    // Note: we cannot remove the client context from myConnectedClients because the following close message
                    //       will be put to the queue. And if it is removed from myConnectedClients then the client context
                    //       would not be found during polling and the close connection message woiuld never be sent to the client.
                    //       
                    //       The removing of the client context works like this:
                    //       The client gets the close connection message. The client processes it and stops polling.
                    //       On the service side the time detects the client sopped polling and so it removes
                    //       the client context from my connected clients.
                }

                if (aClientContext != null)
                {
                    CloseConnection(aClientContext);
                }
            }
        }

        private void HandleConnection(HttpListenerContext httpRequestContext)
        {
            using (EneterTrace.Entering())
            {
                // If polling. (when client polls to get response messages)
                if (httpRequestContext.Request.HttpMethod.ToUpperInvariant() == "GET")
                {
                    // Get responseReceiverId.
                    string[] aQueryItems = httpRequestContext.Request.Url.Query.Split('&');
                    if (aQueryItems.Length > 0)
                    {
                        string aResponseReceiverId = aQueryItems[0].Substring(4);

                        // Find the client.
                        HttpResponseSender aClientContext;
                        using (ThreadLock.Lock(myConnectedClients))
                        {
                            aClientContext = myConnectedClients.FirstOrDefault(x => x.ResponseReceiverId == aResponseReceiverId);
                        }

                        if (aClientContext != null)
                        {
                            // Response collected messages.
                            byte[] aMessages = aClientContext.DequeueCollectedMessages();
                            if (aMessages != null)
                            {
                                httpRequestContext.SendResponseMessage(aMessages);
                            }
                        }
                        else
                        {
                            // Note: This happens when the polling runs and the connection is not open yet.
                            //       It is a normal situation because the polling thread on the client starts
                            //       slightly before the connection is open.
                        }
                    }
                    else
                    {
                        EneterTrace.Warning("Incorrect query format detected for HTTP GET request.");

                        // The request was not processed.
                        httpRequestContext.Response.StatusCode = 404;
                    }
                }
                else
                // Client sends a request message.
                {
                    byte[] aMessage = httpRequestContext.GetRequestMessage();

                    IPEndPoint anEndPoint = httpRequestContext.Request.RemoteEndPoint;
                    string aClientIp = (anEndPoint != null) ? anEndPoint.Address.ToString() : "";

                    ProtocolMessage aProtocolMessage = myProtocolFormatter.DecodeMessage(aMessage);

                    bool anIsProcessingOk = true;
                    if (aProtocolMessage != null && !string.IsNullOrEmpty(aProtocolMessage.ResponseReceiverId))
                    {
                        MessageContext aMessageContext = new MessageContext(aProtocolMessage, aClientIp);

                        if (aProtocolMessage.MessageType == EProtocolMessageType.OpenConnectionRequest)
                        {
                            using (ThreadLock.Lock(myConnectedClients))
                            {
                                HttpResponseSender aClientContext = myConnectedClients.FirstOrDefault(x => x.ResponseReceiverId == aProtocolMessage.ResponseReceiverId);
                                if (aClientContext != null && aClientContext.IsDisposed)
                                {
                                    // The client with the same id exists but was closed and disposed.
                                    // It is just that the timer did not remove it. So delete it now.
                                    myConnectedClients.Remove(aClientContext);

                                    // Indicate the new client context shall be created.
                                    aClientContext = null;
                                }

                                if (aClientContext == null)
                                {
                                    aClientContext = new HttpResponseSender(aProtocolMessage.ResponseReceiverId, aClientIp);
                                    myConnectedClients.Add(aClientContext);

                                    // If this is the only sender then start the timer measuring the inactivity to detect if the client is disconnected.
                                    // If it is not the only sender, then the timer is already running.
                                    if (myConnectedClients.Count == 1)
                                    {
                                        myResponseReceiverInactivityTimer.Change(myResponseReceiverInactivityTimeout, -1);
                                    }
                                }
                                else
                                {
                                    EneterTrace.Warning(TracedObject + "could not open connection for client '" + aProtocolMessage.ResponseReceiverId + "' because the client with same id is already connected.");
                                    anIsProcessingOk = false;
                                }
                            }
                        }
                        else if (aProtocolMessage.MessageType == EProtocolMessageType.CloseConnectionRequest)
                        {
                            using (ThreadLock.Lock(myConnectedClients))
                            {
                                HttpResponseSender aClientContext = myConnectedClients.FirstOrDefault(x => x.ResponseReceiverId == aProtocolMessage.ResponseReceiverId);

                                if (aClientContext != null)
                                {
                                    // Note: the disconnection comes from the client.
                                    //       It means the client closed the connection and will not poll anymore.
                                    //       Therefore the client context can be removed.
                                    myConnectedClients.Remove(aClientContext);
                                    aClientContext.Dispose();
                                }
                            }
                        }

                        if (anIsProcessingOk)
                        {
                            NotifyMessageContext(aMessageContext);
                        }
                    }
                    
                    if (!anIsProcessingOk)
                    {
                        // The request was not processed.
                        httpRequestContext.Response.StatusCode = 404;
                    }
                }
            }
        }

        private void CloseConnection(HttpResponseSender clientContext)
        {
            using (EneterTrace.Entering())
            {
                if (!clientContext.IsDisposed)
                {
                    try
                    {
                        // Send close connection message.
                        object anEncodedMessage = myProtocolFormatter.EncodeCloseConnectionMessage(clientContext.ResponseReceiverId);
                        clientContext.SendResponseMessage(anEncodedMessage);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning("failed to send the close message.", err);
                    }

                    // Note: the client context will be removed by the timer.
                    //       The reason is the client can still poll for messages which are stored in the HttpResponseSender.
                    clientContext.Dispose();
                }
            }
        }

        private void OnConnectionCheckTimer(object o)
        {
            using (EneterTrace.Entering())
            {
                List<HttpResponseSender> aClientsToNotify = new List<HttpResponseSender>();
                bool aStartTimerFlag = false;

                using (ThreadLock.Lock(myConnectedClients))
                {
                    DateTime aTime = DateTime.Now;

                    // Check the connection for each connected duplex output channel.
                    myConnectedClients.RemoveWhere(x =>
                        {
                            // If the last polling activity time exceeded the maximum allowed time then
                            // it is considered the connection is closed.
                            if (aTime - x.LastPollingActivityTime >= TimeSpan.FromMilliseconds(myResponseReceiverInactivityTimeout))
                            {
                                // If the connection was broken unexpectidly then the message handler must be notified.
                                if (!x.IsDisposed)
                                {
                                    aClientsToNotify.Add(x);
                                }

                                // Indicate to remove the item.
                                return true;
                            }

                            // Indicate to keep the item.
                            return false;
                        });

                    // If there connected clients we need to check if they are active.
                    if (myConnectedClients.Count > 0)
                    {
                        aStartTimerFlag = true;
                    }
                }

                foreach (HttpResponseSender aClientContext in aClientsToNotify)
                {
                    ProtocolMessage aProtocolMessage = new ProtocolMessage(EProtocolMessageType.CloseConnectionRequest, aClientContext.ResponseReceiverId, null);
                    MessageContext aMessageContext = new MessageContext(aProtocolMessage, aClientContext.ClientIp);
                    NotifyMessageContext(aMessageContext);
                }

                if (aStartTimerFlag)
                {
                    myResponseReceiverInactivityTimer.Change(myResponseReceiverInactivityTimeout, -1);
                }
            }
        }

        private void NotifyMessageContext(MessageContext messageContext)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    Action<MessageContext> aMessageHandler = myMessageHandler;
                    if (aMessageHandler != null)
                    {
                        aMessageHandler(messageContext);
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                }
            }
        }


        private IProtocolFormatter myProtocolFormatter;
        private HttpWebServer myHttpListenerProvider;
        private Action<MessageContext> myMessageHandler;
        private object myListeningManipulatorLock = new object();
        private Timer myResponseReceiverInactivityTimer;
        private int myResponseReceiverInactivityTimeout;
        private HashSet<HttpResponseSender> myConnectedClients = new HashSet<HttpResponseSender>();

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}