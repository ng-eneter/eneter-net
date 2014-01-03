/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.HttpMessagingSystem
{
    internal class HttpInputConnector : IInputConnector
    {
        private class HttpResponseSender : ISender, IDisposable
        {
            public HttpResponseSender(string responseReceiverId)
            {
                ResponseReceiverId = responseReceiverId;
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

            public bool IsStreamWritter { get { return false; } }

            public void SendMessage(object message)
            {
                using (EneterTrace.Entering())
                {
                    if (IsDisposed)
                    {
                        throw new ObjectDisposedException(GetType().Name);
                    }

                    lock (myMessages)
                    {
                        byte[] aMessage = (byte[])message;
                        myMessages.Enqueue(aMessage);
                    }
                }
            }

            public void SendMessage(Action<Stream> toStreamWritter)
            {
                throw new NotSupportedException("Http ResponseSender is not a stream sender.");
            }


            // Note: this method must be available after the dispose.
            public byte[] DequeueCollectedMessages()
            {
                using (EneterTrace.Entering())
                {
                    byte[] aDequedMessages = null;

                    lock (myMessages)
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
            public DateTime LastPollingActivityTime { get; private set; }
            public bool IsDisposed { get; private set; }
            
            private Queue<byte[]> myMessages = new Queue<byte[]>();
        }

        public HttpInputConnector(string httpAddress, int responseReceiverInactivityTimeout)
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
                myHttpListenerProvider = new HttpListenerProvider(aUri.AbsoluteUri);

                myResponseReceiverInactivityTimeout = responseReceiverInactivityTimeout;


                // Initialize the timer to regularly check the timeout for connections with duplex output channels.
                // If the duplex output channel did not poll within the timeout then the connection
                // is closed and removed from the list.
                // Note: The timer is set here but not executed.
                myResponseReceiverInactivityTimer = new Timer(OnConnectionCheckTimer, null, -1, -1);
            }
        }

        public void StartListening(Func<MessageContext, bool> messageHandler)
        {
            using (EneterTrace.Entering())
            {
                myMessageHandler = messageHandler;
                myHttpListenerProvider.StartListening(HandleConnection);
            }
        }

        public void StopListening()
        {
            using (EneterTrace.Entering())
            {
                myHttpListenerProvider.StopListening();
                myMessageHandler = null;
            }
        }

        public bool IsListening { get { return myHttpListenerProvider.IsListening; } }

        public ISender CreateResponseSender(string responseReceiverAddress)
        {
            using (EneterTrace.Entering())
            {
                lock (myResponseSenders)
                {
                    // If there are some disposed senders then remove them.
                    myResponseSenders.RemoveWhere(x => x.IsDisposed);

                    // If does not exist create one.
                    HttpResponseSender aResponseSender = myResponseSenders.FirstOrDefault(x => x.ResponseReceiverId == responseReceiverAddress);
                    if (aResponseSender == null)
                    {
                        aResponseSender = new HttpResponseSender(responseReceiverAddress);
                        myResponseSenders.Add(aResponseSender);

                        // If this is the only sender then start the timer measuring the inactivity to detect if the client is disconnected.
                        // If it is not the only sender, then the timer is already running.
                        if (myResponseSenders.Count == 1)
                        {
                            myResponseReceiverInactivityTimer.Change(myResponseReceiverInactivityTimeout, -1);
                        }
                    }

                    return aResponseSender;
                }
            }
        }


        private void HandleConnection(HttpRequestContext httpRequestContext)
        {
            using (EneterTrace.Entering())
            {
                // If polling.
                if (httpRequestContext.HttpMethod == "GET")
                {
                    // Get responseReceiverId.
                    string[] aQueryItems = httpRequestContext.Uri.Query.Split('&');
                    if (aQueryItems.Length > 0)
                    {
                        string aResponseReceiverId = aQueryItems[0].Substring(4);

                        // Find the sender for the response receiver.
                        HttpResponseSender aResponseSender = null;
                        lock (myResponseSenders)
                        {
                            aResponseSender = myResponseSenders.FirstOrDefault(x => x.ResponseReceiverId == aResponseReceiverId);
                        }

                        if (aResponseSender != null)
                        {
                            // Response collected messages.
                            byte[] aMessages = aResponseSender.DequeueCollectedMessages();
                            if (aMessages != null)
                            {
                                httpRequestContext.Response(aMessages);
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
                        httpRequestContext.ResponseError(404);
                    }
                }
                else
                {
                    byte[] aMessage = httpRequestContext.GetRequestMessage();

                    IPEndPoint anEndPoint = httpRequestContext.RemoteEndPoint as IPEndPoint;
                    string aClientIp = (anEndPoint != null) ? anEndPoint.Address.ToString() : "";
                    MessageContext aMessageContext = new MessageContext(aMessage, aClientIp, null);

                    if (!myMessageHandler(aMessageContext))
                    {
                        // The request was not processed.
                        httpRequestContext.ResponseError(404);
                    }
                }
            }
        }

        private void OnConnectionCheckTimer(object o)
        {
            using (EneterTrace.Entering())
            {
                lock (myResponseSenders)
                {
                    DateTime aTime = DateTime.Now;

                    // Check the connection for each connected duplex output channel.
                    myResponseSenders.RemoveWhere(x =>
                        {
                            // If the last polling activity time exceeded the maximum allowed time then
                            // it is considered the connection is closed.
                            if (aTime - x.LastPollingActivityTime >= TimeSpan.FromMilliseconds(myResponseReceiverInactivityTimeout))
                            {
                                // If the connection was broken unexpectidly then the message handler must be notified.
                                if (!x.IsDisposed)
                                {
                                    MessageContext aMessageContext = new MessageContext(null, "", x);
                                    myMessageHandler(aMessageContext);
                                }

                                // Indicate to remove the item.
                                return true;
                            }

                            // Indicate to keep the item.
                            return false;
                        });

                    // If there connected clients we need to check if they are active.
                    if (myResponseSenders.Count > 0)
                    {
                        myResponseReceiverInactivityTimer.Change(myResponseReceiverInactivityTimeout, -1);
                    }
                }
            }
        }


        private HttpListenerProvider myHttpListenerProvider;
        private Func<MessageContext, bool> myMessageHandler;
        private HashSet<HttpResponseSender> myResponseSenders = new HashSet<HttpResponseSender>();
        private Timer myResponseReceiverInactivityTimer;
        private int myResponseReceiverInactivityTimeout;
    }
}

#endif