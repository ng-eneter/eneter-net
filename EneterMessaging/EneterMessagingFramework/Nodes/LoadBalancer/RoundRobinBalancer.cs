/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System;
using System.Collections.Generic;
using System.Linq;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using System.Threading;
using Eneter.Messaging.Threading;
using Eneter.Messaging.Utils.Collections;

namespace Eneter.Messaging.Nodes.LoadBalancer
{
    internal class RoundRobinBalancer : AttachableDuplexInputChannelBase, ILoadBalancer
    {
        // Represents a connection with a client sending requests via the load balancer.
        private class TConnection
        {
            public TConnection(string responseReceiverId, string senderAddress, IDuplexOutputChannel duplexOutputChannel)
            {
                // Id of the client connected to the load balancer.
                ResponseReceiverId = responseReceiverId;

                SenderAddress = senderAddress;

                // Duplex output channel associated with the client to forward messages to one of receivers.
                DuplexOutputChannel = duplexOutputChannel;
            }

            public string ResponseReceiverId { get; private set; }
            public string SenderAddress { get; private set; }
            public IDuplexOutputChannel DuplexOutputChannel { get; private set; }
        }

        
        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;
        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;
        public event EventHandler<RequestReceiverRemovedEventArgs> RequestReceiverRemoved;


        public RoundRobinBalancer(IMessagingSystemFactory outputMessagingFactory)
        {
            using (EneterTrace.Entering())
            {
                myOutputMessagingFactory = outputMessagingFactory;
            }
        }

        // Adds the request receiver to the pool.
        public void AddDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myReceiverManipulatorLock))
                {
                    if (!myAvailableReceivers.Contains(channelId))
                    {
                        myAvailableReceivers.Add(channelId);
                    }
                }
            }
        }

        // Removes the request receiver to the pool.
        public void RemoveDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myReceiverManipulatorLock))
                {
                    myAvailableReceivers.Remove(channelId);

                    // Close all open connection with this request receiver.
                    myOpenConnections.RemoveWhere(x =>
                        {
                            if (x.DuplexOutputChannel.ChannelId == channelId)
                            {
                                try
                                {
                                    // Close connection with the request receiver.
                                    x.DuplexOutputChannel.CloseConnection();
                                }
                                catch (Exception err)
                                {
                                    EneterTrace.Warning(TracedObject + "failed to close connection to " + channelId, err);
                                }

                                x.DuplexOutputChannel.ConnectionClosed -= OnRequestReceiverClosedConnection;
                                x.DuplexOutputChannel.ResponseMessageReceived -= OnResponseMessageReceived;

                                return true;
                            }

                            return false;
                        });
                }
            }
        }

        // Removes all request receivers.
        public void RemoveAllDuplexOutputChannels()
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myReceiverManipulatorLock))
                {
                    myAvailableReceivers.Clear();

                    // Close all open connections with request receivers.
                    foreach (TConnection aConnection in myOpenConnections)
                    {
                        try
                        {
                            aConnection.DuplexOutputChannel.CloseConnection();
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + "failed to close connection to " + aConnection.DuplexOutputChannel.ChannelId, err);
                        }

                        aConnection.DuplexOutputChannel.ResponseMessageReceived -= OnResponseMessageReceived;
                        aConnection.DuplexOutputChannel.ConnectionClosed -= OnRequestReceiverClosedConnection;
                    }

                    // Clear all open connections.
                    myOpenConnections.Clear();

                    // Note: Clients (response receivers) stay connected becaue it is still possible to add
                    //       new request receivers to the pool.
                }
            }
        }

        // A message from a client is received. It must be forwarded to a request receiver from the pool.
        protected override void OnRequestMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                while (true)
                {
                    string aChannelId;

                    // Get the next available receiver.
                    using (ThreadLock.Lock(myReceiverManipulatorLock))
                    {
                        if (myAvailableReceivers.Count == 0)
                        {
                            EneterTrace.Error(TracedObject + "failed to forward the message because there no receivers in the pool.");
                            break;
                        }

                        // Move to the next receiver.
                        ++myCurrentAvailableReceiverIdx;
                        if (myCurrentAvailableReceiverIdx >= myAvailableReceivers.Count)
                        {
                            myCurrentAvailableReceiverIdx = 0;
                        }

                        aChannelId = myAvailableReceivers[myCurrentAvailableReceiverIdx];

                        // If there is not open connection for the current response receiver id, then open it.
                        TConnection aConnection = myOpenConnections.FirstOrDefault(x => x.ResponseReceiverId == e.ResponseReceiverId && x.DuplexOutputChannel.ChannelId == aChannelId);
                        if (aConnection == null)
                        {
                            IDuplexOutputChannel anOutputChannel = myOutputMessagingFactory.CreateDuplexOutputChannel(aChannelId);
                            anOutputChannel.ResponseMessageReceived += OnResponseMessageReceived;
                            anOutputChannel.ConnectionClosed += OnRequestReceiverClosedConnection;

                            try
                            {
                                anOutputChannel.OpenConnection();
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.FailedToOpenConnection, err);

                                anOutputChannel.ResponseMessageReceived -= OnResponseMessageReceived;
                                anOutputChannel.ConnectionClosed -= OnRequestReceiverClosedConnection;

                                // It was not possible to open the connection with the given receiver.
                                // So remove the receiver from available receivers.
                                myAvailableReceivers.Remove(aChannelId);

                                NotifyRequestRecieverRemoved(aChannelId);

                                // Continue with the next receiver.
                                continue;
                            }

                            aConnection = new TConnection(e.ResponseReceiverId, e.SenderAddress, anOutputChannel);
                            myOpenConnections.Add(aConnection);
                        }


                        // Try to forward the message via the connection.
                        try
                        {
                            aConnection.DuplexOutputChannel.SendMessage(e.Message);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.FailedToSendMessage, err);

                            try
                            {
                                // Remove the connection.
                                aConnection.DuplexOutputChannel.CloseConnection();
                            }
                            catch (Exception err2)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.FailedToCloseConnection, err2);
                            }

                            aConnection.DuplexOutputChannel.ResponseMessageReceived -= OnResponseMessageReceived;
                            aConnection.DuplexOutputChannel.ConnectionClosed -= OnRequestReceiverClosedConnection;

                            myOpenConnections.Remove(aConnection);

                            // Try next receiver.
                            continue;
                        }

                        break;
                    }

                }
            }
        }

        // A request receiver disconnected one client.
        private void OnRequestReceiverClosedConnection(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                // Response receiver disconected one client so remove the connection.
                using (ThreadLock.Lock(myReceiverManipulatorLock))
                {
                    myOpenConnections.RemoveWhere(x =>
                    {
                        if (x.DuplexOutputChannel.ResponseReceiverId == e.ResponseReceiverId)
                        {
                            x.DuplexOutputChannel.ResponseMessageReceived -= OnResponseMessageReceived;
                            x.DuplexOutputChannel.ConnectionClosed -= OnRequestReceiverClosedConnection;

                            return true;
                        }

                        return false;
                    });
                }
            }
        }

        // A response message is received from the request receiver.
        private void OnResponseMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myReceiverManipulatorLock))
                {
                    // Find the connection associated with the ResponseRecieverId to which this response message
                    // was delivered.
                    TConnection aConnection = myOpenConnections.FirstOrDefault(x => x.DuplexOutputChannel.ResponseReceiverId == e.ResponseReceiverId);
                    if (aConnection == null)
                    {
                        EneterTrace.Warning(TracedObject + "could not find the receiver for the incoming response message.");
                        return;
                    }

                    // Forward the response message to the client.
                    IDuplexInputChannel anInputChannel = AttachedDuplexInputChannel;
                    if (anInputChannel != null)
                    {
                        try
                        {
                            anInputChannel.SendResponseMessage(aConnection.ResponseReceiverId, e.Message);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Error(TracedObject + ErrorHandler.FailedToSendResponseMessage, err);

                            try
                            {
                                // Forwarding the response to the client failed so disconnect the client.
                                anInputChannel.DisconnectResponseReceiver(aConnection.ResponseReceiverId);
                            }
                            catch (Exception err2)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.FailedToDisconnectResponseReceiver + aConnection.ResponseReceiverId, err2);
                            }

                            myOpenConnections.RemoveWhere(x =>
                                {
                                    if (x.ResponseReceiverId == aConnection.ResponseReceiverId)
                                    {
                                        try
                                        {
                                            x.DuplexOutputChannel.CloseConnection();
                                        }
                                        catch (Exception err2)
                                        {
                                            EneterTrace.Warning(TracedObject + ErrorHandler.FailedToCloseConnection, err2);
                                        }

                                        x.DuplexOutputChannel.ResponseMessageReceived -= OnResponseMessageReceived;
                                        x.DuplexOutputChannel.ConnectionClosed -= OnRequestReceiverClosedConnection;

                                        return true;
                                    }

                                    return false;
                                });

                            EneterThreadPool.QueueUserWorkItem(() => Notify(ResponseReceiverDisconnected, new ResponseReceiverEventArgs(aConnection.ResponseReceiverId, aConnection.SenderAddress)));
                        }
                    }
                    else
                    {
                        EneterTrace.Error(TracedObject + "cannot send the response message when the duplex input channel is not attached.");
                    }
                }
            }
        }

        // A client was connected the load balancer.
        protected override void OnResponseReceiverConnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                Notify(ResponseReceiverConnected, e);
            }
        }

        // A client was disconnected from the load balancer.
        protected override void OnResponseReceiverDisconnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myReceiverManipulatorLock))
                {
                    myOpenConnections.RemoveWhere(x =>
                        {
                            if (x.ResponseReceiverId == e.ResponseReceiverId)
                            {
                                try
                                {
                                    x.DuplexOutputChannel.CloseConnection();
                                }
                                catch (Exception err)
                                {
                                    EneterTrace.Warning(TracedObject + ErrorHandler.FailedToCloseConnection, err);
                                }

                                x.DuplexOutputChannel.ResponseMessageReceived -= OnResponseMessageReceived;
                                x.DuplexOutputChannel.ConnectionClosed -= OnRequestReceiverClosedConnection;

                                return true;
                            }

                            return false;
                        });
                }

                Notify(ResponseReceiverDisconnected, e);
            }
        }

        private string GetNextAvailableReceiver()
        {
            using (ThreadLock.Lock(myDuplexInputChannelManipulatorLock))
            {
                // Move to the next receiver.
                ++myCurrentAvailableReceiverIdx;
                if (myCurrentAvailableReceiverIdx >= myAvailableReceivers.Count)
                {
                    myCurrentAvailableReceiverIdx = 0;
                }

                return myAvailableReceivers[myCurrentAvailableReceiverIdx];
            }
        }

        private void NotifyRequestRecieverRemoved(string channelId)
        {
            using (EneterTrace.Entering())
            {
                EneterThreadPool.QueueUserWorkItem(() =>
                    {
                        if (RequestReceiverRemoved != null)
                        {
                            try
                            {
                                RequestReceiverRemovedEventArgs anEvent = new RequestReceiverRemovedEventArgs(channelId);
                                RequestReceiverRemoved(this, anEvent);
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                            }
                        }
                    });
            }
        }

        private void Notify(EventHandler<ResponseReceiverEventArgs> handler, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (handler != null)
                {
                    try
                    {
                        handler(this, e);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }


        private object myReceiverManipulatorLock = new object();
        private IMessagingSystemFactory myOutputMessagingFactory;
        private List<string> myAvailableReceivers = new List<string>();
        private int myCurrentAvailableReceiverIdx;
        private List<TConnection> myOpenConnections = new List<TConnection>();

        protected override string TracedObject
        {
            get
            {
                return GetType().Name + " ";
            }
        }
    }
}