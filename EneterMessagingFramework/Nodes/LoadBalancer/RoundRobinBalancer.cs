/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.Linq;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.Nodes.LoadBalancer
{
    internal class RoundRobinBalancer : AttachableDuplexInputChannelBase, ILoadBalancer
    {
        private class TReceiver
        {
            public class TConnection
            {
                public TConnection(string responseReceiverId, IDuplexOutputChannel duplexOutputChannel)
                {
                    ResponseReceiverId = responseReceiverId;
                    DuplexOutputChannel = duplexOutputChannel;
                }

                public string ResponseReceiverId { get; private set; }
                public IDuplexOutputChannel DuplexOutputChannel { get; private set; }
            }

            public TReceiver(string duplexOutputChannelId)
            {
                ChannelId = duplexOutputChannelId;
            }

            public string ChannelId { get; private set; }
            public HashSet<TConnection> OpenConnections { get { return myOpenConnections; } }

            private HashSet<TConnection> myOpenConnections = new HashSet<TConnection>();
        }

        
        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;

        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;


        public RoundRobinBalancer(IMessagingSystemFactory outputMessagingFactory)
        {
            using (EneterTrace.Entering())
            {
                myOutputMessagingFactory = outputMessagingFactory;
            }
        }


        public void AddDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                lock (myAvailableReceivers)
                {
                    TReceiver aReceiver = new TReceiver(channelId);
                    myAvailableReceivers.Add(aReceiver);
                }
            }
        }

        public void RemoveDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                lock (myAvailableReceivers)
                {
                    // Find the receiver with the given channel id.
                    TReceiver aReceiver = myAvailableReceivers.FirstOrDefault(x => x.ChannelId == channelId);
                    if (aReceiver != null)
                    {
                        // Try to close all open duplex output channels.
                        foreach (TReceiver.TConnection aConnection in aReceiver.OpenConnections)
                        {
                            try
                            {
                                aConnection.DuplexOutputChannel.CloseConnection();
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + "failed to close connection to " + channelId, err);
                            }

                            aConnection.DuplexOutputChannel.ResponseMessageReceived -= OnResponseMessageReceived;
                        }

                        // Remove the connection from available cnnections.
                        myAvailableReceivers.Remove(aReceiver);
                    }
                }
            }
        }

        public void RemoveAllDuplexOutputChannels()
        {
            using (EneterTrace.Entering())
            {
                lock (myAvailableReceivers)
                {
                    // Go via all available receivers.
                    foreach (TReceiver aReceiver in myAvailableReceivers)
                    {
                        // Try to close all open duplex output channels.
                        foreach (TReceiver.TConnection aConnection in aReceiver.OpenConnections)
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
                        }
                    }

                    // Clean available receivers.
                    myAvailableReceivers.Clear();
                }
            }
        }


        protected override void OnMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                lock (myAvailableReceivers)
                {
                    if (myAvailableReceivers.Count == 0)
                    {
                        EneterTrace.Warning(TracedObject + " could not forward the request because there are no attached duplex output channels.");
                        return;
                    }

                    // Try to forward the incoming message to the first available receiver.
                    for (int i = 0; i < myAvailableReceivers.Count; ++i)
                    {
                        TReceiver aReceiver = myAvailableReceivers[i];

                        // If there is not open connection for the current response receiver id, then open it.
                        TReceiver.TConnection aConnection = aReceiver.OpenConnections.FirstOrDefault(x => x.ResponseReceiverId == e.ResponseReceiverId);
                        if (aConnection == null)
                        {
                            IDuplexOutputChannel anOutputChannel = myOutputMessagingFactory.CreateDuplexOutputChannel(aReceiver.ChannelId);
                            aConnection = new TReceiver.TConnection(e.ResponseReceiverId, anOutputChannel);

                            try
                            {
                                aConnection.DuplexOutputChannel.ResponseMessageReceived += OnResponseMessageReceived;
                                aConnection.DuplexOutputChannel.OpenConnection();

                                aReceiver.OpenConnections.Add(aConnection);
                            }
                            catch (Exception err)
                            {
                                aConnection.DuplexOutputChannel.ResponseMessageReceived -= OnResponseMessageReceived;
                                EneterTrace.Warning(TracedObject + ErrorHandler.OpenConnectionFailure, err);
                                
                                // Try the next connection.
                                continue;
                            }
                        }

                        // Forward the message to the "service".
                        try
                        {
                            aConnection.DuplexOutputChannel.SendMessage(e.Message);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.SendMessageFailure, err);

                            // Try the next connection.
                            continue;
                        }

                        // Put the used receiver to the end.
                        myAvailableReceivers.RemoveAt(i);
                        myAvailableReceivers.Add(aReceiver);

                        // The sending was successful.
                        break;
                    }
                }
            }
        }

        private void OnResponseMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                string aResponseReceiverId = null;

                lock (myAvailableReceivers)
                {
                    // The response receiver id comming with the message belongs to the duplex output channel.
                    // So we need to find the responce receiver id for the duplex input channel.
                    TReceiver aReceiver = myAvailableReceivers.FirstOrDefault(x => x.ChannelId == e.ChannelId);
                    if (aReceiver != null)
                    {
                        TReceiver.TConnection aConnection = aReceiver.OpenConnections.FirstOrDefault(x => x.DuplexOutputChannel.ResponseReceiverId == e.ResponseReceiverId);
                        if (aConnection != null)
                        {
                            aResponseReceiverId = aConnection.ResponseReceiverId;
                        }
                    }
                }

                if (aResponseReceiverId == null)
                {
                    EneterTrace.Warning(TracedObject + "could not find receiver for the incoming response message.");
                    return;
                }

                lock (myDuplexInputChannelManipulatorLock)
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
                            EneterTrace.Error(TracedObject + ErrorHandler.SendResponseFailure, err);
                        }
                    }
                    else
                    {
                        EneterTrace.Error(TracedObject + "cannot send the response message when the duplex input channel is not attached.");
                    }
                }
            }
        }

        protected override void OnResponseReceiverConnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (ResponseReceiverConnected != null)
                {
                    try
                    {
                        ResponseReceiverConnected(this, e);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }

        protected override void OnResponseReceiverDisconnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                lock (myAvailableReceivers)
                {
                    // Go via all available receivers and close all open channels for the disconnecting response receiver.
                    foreach (TReceiver aReceiver in myAvailableReceivers)
                    {
                        // Try to close all the open duplex output channel for the disconnecting response receiver
                        TReceiver.TConnection aConnection = aReceiver.OpenConnections.FirstOrDefault(x => x.ResponseReceiverId == e.ResponseReceiverId);
                        if (aConnection != null)
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

                            aReceiver.OpenConnections.Remove(aConnection);
                        }
                    }

                    if (ResponseReceiverDisconnected != null)
                    {
                        try
                        {
                            ResponseReceiverDisconnected(this, e);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                        }
                    }
                }
            }
        }


        private IMessagingSystemFactory myOutputMessagingFactory;
        private List<TReceiver> myAvailableReceivers = new List<TReceiver>();

        protected override string TracedObject
        {
            get
            {
                return "The DuplexDistributor ";
            }
        }
    }
}


#endif