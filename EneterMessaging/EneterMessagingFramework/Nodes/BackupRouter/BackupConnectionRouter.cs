/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.Threading;

namespace Eneter.Messaging.Nodes.BackupRouter
{
    internal class BackupConnectionRouter : AttachableDuplexInputChannelBase, IBackupConnectionRouter
    {
        // Prepresents a connection between the client connected to this Backup Router and the message
        // receiver behind the router.
        private class TConnection
        {
            public TConnection(string responseReceiverId, IDuplexOutputChannel duplexOutputChannel)
            {
                ResponseReceiverId = responseReceiverId;
                DuplexOutputChannel = duplexOutputChannel;
            }

            // Id of the client connected to the backup router.
            public string ResponseReceiverId { get; private set; }

            // Output channel associated with the client id.
            public IDuplexOutputChannel DuplexOutputChannel { get; private set; }
        }


        public event EventHandler<RedirectEventArgs> ConnectionRedirected;
        public event EventHandler AllRedirectionsFailed;


        public BackupConnectionRouter(IMessagingSystemFactory outputMessagingFactory)
        {
            using (EneterTrace.Entering())
            {
                myOutputMessagingFactory = outputMessagingFactory;
            }
        }

        public IEnumerable<string> AvailableReceivers
        {
            get
            {
                using (ThreadLock.Lock(myConnectionsLock))
                {
                    return myAvailableReceivers.ToArray();
                }
            }
        }

        public void AddReceiver(string channelId)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myConnectionsLock))
                {
                    if (!myAvailableReceivers.Any(x => x == channelId))
                    {
                        myAvailableReceivers.Add(channelId);
                    }
                }
            }
        }

        public void AddReceivers(IEnumerable<string> channelIds)
        {
            using (EneterTrace.Entering())
            {
                foreach (string aChannelId in channelIds)
                {
                    AddReceiver(aChannelId);
                }
            }
        }

        public void RemoveReceiver(string channelId)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myConnectionsLock))
                {
                    // Find all open connections with this receiver then close them and remove from the list.
                    List<string> aClientsToBeRedirected = new List<string>();

                    myOpenConnections.RemoveAll(x =>
                    {
                        if (x.DuplexOutputChannel.ChannelId == channelId)
                        {
                            aClientsToBeRedirected.Add(x.ResponseReceiverId);

                            CloseConnection(x);

                            // Indicate it can be removed from the list.
                            return true;
                        }

                        return false;
                    });

                    // Remove the receiver from the list of available receivers.
                    myAvailableReceivers.Remove(channelId);

                    if (myAvailableReceiverIdx >= myAvailableReceivers.Count)
                    {
                        myAvailableReceiverIdx = Math.Max(0, myAvailableReceivers.Count - 1);
                    }

                    // If there are still available receivers then try to redirect closed connections.
                    foreach (string aClientId in aClientsToBeRedirected)
                    {
                        TConnection aConnection = OpenConnection(aClientId);
                        myOpenConnections.Add(aConnection);
                    }
                }
            }
        }

        /// <summary>
        /// Cleans the list with available receivers. Existing connections are not broken.
        /// </summary>
        public void RemoveAllReceivers()
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myConnectionsLock))
                {
                    // Close all connections.
                    myOpenConnections.ForEach(x => CloseConnection(x));
                    myOpenConnections.Clear();

                    myAvailableReceivers.Clear();
                    myAvailableReceiverIdx = 0;
                }
            }
        }

        /// <summary>
        /// It is called when a client opens the connection to this backup router.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnResponseReceiverConnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myConnectionsLock))
                {
                    // Open the associated connection with the service for the incoming client.
                    TConnection aConnection = OpenConnection(e.ResponseReceiverId);
                    myOpenConnections.Add(aConnection);
                }
            }
        }

        /// <summary>
        /// It is called when the client actively closed the connection with this backup router.
        /// It will close the associated connection with the service.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnResponseReceiverDisconnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myConnectionsLock))
                {
                    TConnection aConnetion = myOpenConnections.FirstOrDefault(x => x.ResponseReceiverId == e.ResponseReceiverId);
                    CloseConnection(aConnetion);
                    myOpenConnections.Remove(aConnetion);
                }
            }
        }

        /// <summary>
        /// It is called when a message is received from a client connected to this Backup Router.
        /// The message will be forwarded to the connected service.
        /// If the sending fails the connection is considered broken it will try to reconnect with the next 
        /// available service and send the message again.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnRequestMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                TConnection aConnection;
                using (ThreadLock.Lock(myConnectionsLock))
                {
                    // If the client does not have associated connection to the service behind the router create it.
                    aConnection = myOpenConnections.FirstOrDefault(x => x.ResponseReceiverId == e.ResponseReceiverId);
                    if (aConnection == null)
                    {
                        aConnection = OpenConnection(e.ResponseReceiverId);
                        myOpenConnections.Add(aConnection);
                    }

                    try
                    {
                        aConnection.DuplexOutputChannel.SendMessage(e.Message);

                        // The message was successfully sent.
                        return;
                    }
                    catch (Exception err)
                    {
                        // Sending of the message failed. Therefore the connection is considered broken.
                        EneterTrace.Warning(TracedObject + "failed to forward the message to " + aConnection.DuplexOutputChannel.ChannelId + ". The connection will be redirected.", err);
                    }

                    // Redirect and try to send again.
                    try
                    {
                        // Close the broken connection.
                        CloseConnection(aConnection);
                        myOpenConnections.Remove(aConnection);

                        // Set next available receiver.
                        SetNextAvailableReceiver();

                        // Open the new connection.
                        TConnection aNewConnection = OpenConnection(e.ResponseReceiverId);
                        myOpenConnections.Add(aNewConnection);

                        NotifyConnectionRedirected(aNewConnection.ResponseReceiverId, aConnection.DuplexOutputChannel.ChannelId, aNewConnection.DuplexOutputChannel.ChannelId);

                        // Send the message via the new connection.
                        aNewConnection.DuplexOutputChannel.SendMessage(e.Message);
                    }
                    catch (Exception err)
                    {
                        string aMessage = TracedObject + "failed to forward the message after the redirection";
                        EneterTrace.Error(aMessage, err);
                        throw;
                    }
                }
            }
        }

        private TConnection OpenConnection(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myConnectionsLock))
                {
                    // If the client is already connected.
                    if (myOpenConnections.Any(x => x.ResponseReceiverId == responseReceiverId))
                    {
                        throw new InvalidOperationException(TracedObject + "failed to open connection. " + responseReceiverId + " is already connected.");
                    }

                    // If there are no available receivers then the connection cannot be open.
                    if (myAvailableReceivers.Count == 0)
                    {
                        string anErrorMessage1 = TracedObject + "cannot open the connection because the list with available receivers is empty.";
                        throw new InvalidOperationException(anErrorMessage1);
                    }

                    // Round Robin loop via available connections starting with the last available connection.
                    for (int i = 0; i < myAvailableReceivers.Count; ++i)
                    {
                        int j = (myAvailableReceiverIdx + i) % myAvailableReceivers.Count;
                        string anAvailableChannelId = myAvailableReceivers[j];

                        IDuplexOutputChannel anOutputChannel = myOutputMessagingFactory.CreateDuplexOutputChannel(anAvailableChannelId);

                        // Subscribe to be notified if the connection is broken.
                        anOutputChannel.ConnectionClosed += OnOutputChannelConnectionClosed;

                        // Subscribe for response messages that must be redirected back to the client.
                        anOutputChannel.ResponseMessageReceived += OnOutputChannelResponseMessageReceived;

                        // Try to open connection.
                        try
                        {
                            anOutputChannel.OpenConnection();

                            // Store index of working output channel.
                            // Note: The next new connection will try this output channel first.
                            myAvailableReceiverIdx = j;

                            // Store connected output channel.
                            TConnection aConnetion = new TConnection(responseReceiverId, anOutputChannel);

                            // Connection was established.
                            return aConnetion;
                        }
                        catch (Exception err)
                        {
                            anOutputChannel.ResponseMessageReceived -= OnOutputChannelResponseMessageReceived;
                            anOutputChannel.ConnectionClosed -= OnOutputChannelConnectionClosed;

                            // The opening failed so it continues with the next available outputchannel.
                            EneterTrace.Warning(TracedObject + "failed to connect to receiver " + anAvailableChannelId, err);
                        }
                    }
                }

                NotifyAllRedirectionsFailed();

                string anErrorMessage2 = TracedObject + "failed to open connection with all available receivers.";
                EneterTrace.Error(anErrorMessage2);
                throw new InvalidOperationException(anErrorMessage2);
            }
        }

        
        private void CloseConnection(TConnection connection)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myConnectionsLock))
                {
                    if (connection != null)
                    {
                        try
                        {
                            connection.DuplexOutputChannel.CloseConnection();
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + "failed to close connection.", err);
                        }

                        connection.DuplexOutputChannel.ResponseMessageReceived -= OnOutputChannelResponseMessageReceived;
                        connection.DuplexOutputChannel.ConnectionClosed -= OnOutputChannelConnectionClosed;
                    }
                }
            }
        }

        /// <summary>
        /// It is called when a response message from the service is received.
        /// The response message must be redirected to the associated client.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnOutputChannelResponseMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                string aResponseReceiverId = null;
                using (ThreadLock.Lock(myConnectionsLock))
                {
                    TConnection aConnection = myOpenConnections.FirstOrDefault(x => x.DuplexOutputChannel.ResponseReceiverId == e.ResponseReceiverId);
                    if (aConnection != null)
                    {
                        aResponseReceiverId = aConnection.ResponseReceiverId;
                    }
                }

                if (aResponseReceiverId == null)
                {
                    EneterTrace.Warning(TracedObject + "could not find receiver for the incoming response message.");
                    return;
                }

                using (ThreadLock.Lock(myDuplexInputChannelManipulatorLock))
                {
                    // Send the response message via the duplex input channel to the sender.
                    if (AttachedDuplexInputChannel != null)
                    {
                        try
                        {
                            AttachedDuplexInputChannel.SendResponseMessage(aResponseReceiverId, e.Message);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Error(TracedObject + ErrorHandler.FailedToSendResponseMessage, err);
                        }
                    }
                    else
                    {
                        EneterTrace.Error(TracedObject + "cannot send the response message when the duplex input channel is not attached.");
                    }
                }
            }
        }

        /// <summary>
        /// It is called when a connection with the receiver is broken.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnOutputChannelConnectionClosed(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                TConnection aNewConnection = null;
                using (ThreadLock.Lock(myConnectionsLock))
                {
                    // Find the client id associated with the duplex output channel which raised this event.
                    // Note: We need to get the response receiver id representing the client connected to this BackupRouter.
                    TConnection aConnection = myOpenConnections.FirstOrDefault(x => x.DuplexOutputChannel.ResponseReceiverId == e.ResponseReceiverId);
                    
                    // If the corresponding client exists.
                    // E.g. the client could close its connection to the backup router in parallel/very short time bofore the connection was broken.
                    if (aConnection != null)
                    {
                        // Close the broken connection.
                        CloseConnection(aConnection);
                        myOpenConnections.Remove(aConnection);

                        // Set the next available receiver.
                        SetNextAvailableReceiver();

                        // Open the new connection.
                        aNewConnection = OpenConnection(aConnection.ResponseReceiverId);
                        myOpenConnections.Add(aNewConnection);
                    }
                }

                if (aNewConnection != null)
                {
                    NotifyConnectionRedirected(aNewConnection.ResponseReceiverId, e.ChannelId, aNewConnection.DuplexOutputChannel.ChannelId);
                }
            }
        }

        private void NotifyConnectionRedirected(string clientId, string from, string to)
        {
            using (EneterTrace.Entering())
            {
                Action aWaitCallback = () =>
                    {
                        using (EneterTrace.Entering())
                        {
                            if (ConnectionRedirected != null)
                            {
                                try
                                {
                                    RedirectEventArgs aMsg = new RedirectEventArgs(clientId, from, to);
                                    ConnectionRedirected(this, aMsg);
                                }
                                catch (Exception err)
                                {
                                    EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                                }
                            }
                        }
                    };
                EneterThreadPool.QueueUserWorkItem(aWaitCallback);
            }
        }

        private void NotifyAllRedirectionsFailed()
        {
            using (EneterTrace.Entering())
            {
                Action aWaitCallback = () =>
                    {
                        using (EneterTrace.Entering())
                        {
                            if (AllRedirectionsFailed != null)
                            {
                                try
                                {
                                    AllRedirectionsFailed(this, new EventArgs());
                                }
                                catch (Exception err)
                                {
                                    EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                                }
                            }
                        }
                    };
                EneterThreadPool.QueueUserWorkItem(aWaitCallback);
            }
        }

        private void SetNextAvailableReceiver()
        {
            using (ThreadLock.Lock(myConnectionsLock))
            {
                myAvailableReceiverIdx = (myAvailableReceiverIdx < myAvailableReceivers.Count) ? myAvailableReceiverIdx + 1 : 0;
            }
        }

        private IMessagingSystemFactory myOutputMessagingFactory;

        private List<string> myAvailableReceivers = new List<string>();
        private int myAvailableReceiverIdx;
        private List<TConnection> myOpenConnections = new List<TConnection>();
        private object myConnectionsLock = new object();


        protected override string TracedObject
        {
            get
            {
                string aDuplexInputChannelId = (AttachedDuplexInputChannel != null) ? AttachedDuplexInputChannel.ChannelId : "";
                return "BackupConnectionRouter attached to the duplex input channel '" + aDuplexInputChannelId + "' ";
            }
        }
    }
}