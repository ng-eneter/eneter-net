/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Eneter.Messaging.Nodes.Dispatcher
{
    internal class DuplexDispatcher : IAttachableMultipleDuplexInputChannels, IDuplexDispatcher
    {
        // Represents one particular client which is connected via the input channel.
        private class TClient
        {
            public TClient(IDuplexInputChannel inputChannel, string inputResponseReceiverId)
            {
                using (EneterTrace.Entering())
                {
                    myInputChannel = inputChannel;
                    myInputResponseReceiverId = inputResponseReceiverId;
                }
            }

            // Client opens connections to all available outputs.
            public void OpenOutputConnections(IMessagingSystemFactory messaging, IEnumerable<string> availableOutputChannelIds)
            {
                using (EneterTrace.Entering())
                {
                    foreach (string aChannelId in availableOutputChannelIds)
                    {
                        OpenOutputConnection(messaging, aChannelId);
                    }
                }
            }

            // Client opens connection to a particular output.
            public void OpenOutputConnection(IMessagingSystemFactory messaging, string channelId)
            {
                using (EneterTrace.Entering())
                {
                    IDuplexOutputChannel anOutputChannel = null;
                    try
                    {
                        using (ThreadLock.Lock(myOutputConnectionLock))
                        {
                            anOutputChannel = messaging.CreateDuplexOutputChannel(channelId);
                            anOutputChannel.ConnectionClosed += OnConnectionClosed;
                            anOutputChannel.ResponseMessageReceived += OnResponseMessageReceived;

                            anOutputChannel.OpenConnection();

                            // Connection is successfuly open so it can be stored.
                            myOpenOutputConnections.Add(anOutputChannel);
                        }
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning("Failed to open connection to '" + channelId + "'.", err);

                        if (anOutputChannel != null)
                        {
                            anOutputChannel.ConnectionClosed -= OnConnectionClosed;
                            anOutputChannel.ResponseMessageReceived -= OnResponseMessageReceived;
                        }

                        throw;
                    }
                }
            }

            // Client closes a particular connection.
            public void CloseOutputConnection(string channelId)
            {
                using (EneterTrace.Entering())
                {
                    using (ThreadLock.Lock(myOutputConnectionLock))
                    {
                        for (int i = myOpenOutputConnections.Count - 1; i >= 0; --i)
                        {
                            if (myOpenOutputConnections[i].ChannelId == channelId)
                            {
                                myOpenOutputConnections[i].CloseConnection();

                                myOpenOutputConnections[i].ConnectionClosed -= OnConnectionClosed;
                                myOpenOutputConnections[i].ResponseMessageReceived -= OnResponseMessageReceived;

                                myOpenOutputConnections.RemoveAt(i);
                            }
                        }
                    }
                }
            }

            // Client closes connections to all output.
            public void CloseOutpuConnections()
            {
                using (EneterTrace.Entering())
                {
                    using (ThreadLock.Lock(myOutputConnectionLock))
                    {
                        foreach (IDuplexOutputChannel anOutputChannel in myOpenOutputConnections)
                        {
                            anOutputChannel.CloseConnection();

                            anOutputChannel.ConnectionClosed -= OnConnectionClosed;
                            anOutputChannel.ResponseMessageReceived -= OnResponseMessageReceived;
                        }
                        myOpenOutputConnections.Clear();
                    }
                }
            }

            // Client forwards the message to all output connections.
            public void ForwardMessage(object message)
            {
                using (EneterTrace.Entering())
                {
                    IDuplexOutputChannel[] anOutputChannels = null;

                    using (ThreadLock.Lock(myOutputConnectionLock))
                    {
                        anOutputChannels = myOpenOutputConnections.ToArray();
                    }

                    // Forward the incoming message to all output channels.
                    foreach (IDuplexOutputChannel anOutputChannel in anOutputChannels)
                    {
                        try
                        {
                            anOutputChannel.SendMessage(message);
                        }
                        catch (Exception err)
                        {
                            // Note: do not rethrow the exception because it woiuld stop forwarding the message to other output channels.
                            EneterTrace.Warning("Failed to send message to '" + anOutputChannel.ChannelId + "'.", err);
                        }
                    }
                }
            }

            public bool IsAssociatedResponseReceiverId(string responseReceiverId)
            {
                using (EneterTrace.Entering())
                {
                    using (ThreadLock.Lock(myOutputConnectionLock))
                    {
                        return myOpenOutputConnections.Any(x => x.ResponseReceiverId == responseReceiverId);
                    }
                }
            }

            // When some output connection was closed/broken.
            private void OnConnectionClosed(object sender, DuplexChannelEventArgs e)
            {
                using (EneterTrace.Entering())
                {
                    using (ThreadLock.Lock(myOutputConnectionLock))
                    {
                        IDuplexOutputChannel anOutputChannel = (IDuplexOutputChannel)sender;
                        anOutputChannel.ConnectionClosed -= OnConnectionClosed;
                        anOutputChannel.ResponseMessageReceived -= OnResponseMessageReceived;

                        myOpenOutputConnections.Remove(anOutputChannel);
                    }
                }
            }

            // When client received a message from an output connection.
            private void OnResponseMessageReceived(object sender, DuplexChannelMessageEventArgs e)
            {
                using (EneterTrace.Entering())
                {
                    try
                    {
                        myInputChannel.SendResponseMessage(myInputResponseReceiverId, e.Message);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning("Failed to send message via the input channel '" + myInputChannel.ChannelId + "'.", err);
                    }
                }
            }

            private string myInputResponseReceiverId;
            private object myOutputConnectionLock = new object();
            private List<IDuplexOutputChannel> myOpenOutputConnections = new List<IDuplexOutputChannel>();
            private IDuplexInputChannel myInputChannel;
        }



        // Maintains one particulat input channel and all its connected clients.
        private class TInputChannelContext : AttachableDuplexInputChannelBase, IAttachableDuplexInputChannel
        {
            public TInputChannelContext(IMessagingSystemFactory messaging, Func<IEnumerable<string>> getOutputChannelIds)
            {
                using (EneterTrace.Entering())
                {
                    myMessaging = messaging;
                    myGetOutputChannelIds = getOutputChannelIds;
                }
            }

            public override void DetachDuplexInputChannel()
            {
                using (EneterTrace.Entering())
                {
                    base.DetachDuplexInputChannel();

                    using (ThreadLock.Lock(myClientConnectionLock))
                    {
                        // Close connections of all clients.
                        foreach (KeyValuePair<string, TClient> aClient in myConnectedClients)
                        {
                            aClient.Value.CloseOutpuConnections();
                        }
                        myConnectedClients.Clear();
                    }
                }
            }

            // Goes via all connected clients and opens the new output connection.
            public void OpenConnection(string channelId)
            {
                using (EneterTrace.Entering())
                {
                    using (ThreadLock.Lock(myClientConnectionLock))
                    {
                        foreach (KeyValuePair<string, TClient> aClient in myConnectedClients)
                        {
                            aClient.Value.OpenOutputConnection(myMessaging, channelId);
                        }
                    }
                }
            }

            // Goes via all connected clients and closes one particular output connection.
            public void CloseConnection(string channelId)
            {
                using (EneterTrace.Entering())
                {
                    using (ThreadLock.Lock(myClientConnectionLock))
                    {
                        foreach (KeyValuePair<string, TClient> aClient in myConnectedClients)
                        {
                            aClient.Value.CloseOutputConnection(channelId);
                        }
                    }
                }
            }

            public string GetAssociatedResponseReceiverId(string responseReceiverId)
            {
                using (EneterTrace.Entering())
                {
                    using (ThreadLock.Lock(myClientConnectionLock))
                    {
                        foreach (KeyValuePair<string, TClient> aClient in myConnectedClients)
                        {
                            if (aClient.Value.IsAssociatedResponseReceiverId(responseReceiverId))
                            {
                                return aClient.Key;
                            }
                        }

                        return null;
                    }
                }
            }

            protected override void OnRequestMessageReceived(object sender, DuplexChannelMessageEventArgs e)
            {
                using (EneterTrace.Entering())
                {
                    TClient aClient;
                    using (ThreadLock.Lock(myClientConnectionLock))
                    {
                        myConnectedClients.TryGetValue(e.ResponseReceiverId, out aClient);
                    }

                    if (aClient != null)
                    {
                        aClient.ForwardMessage(e.Message);
                    }
                    else
                    {
                        EneterTrace.Warning(TracedObject + "failed to forward the message because ResponseReceiverId '" + e.ResponseReceiverId + "' was not found among open connections.");
                    }
                }
            }

            protected override void OnResponseReceiverConnected(object sender, ResponseReceiverEventArgs e)
            {
                using (EneterTrace.Entering())
                {
                    TClient aNewClient = new TClient(AttachedDuplexInputChannel, e.ResponseReceiverId);
                    IEnumerable<string> anOutputChannelIds = myGetOutputChannelIds();

                    using (ThreadLock.Lock(myClientConnectionLock))
                    {
                        // Opens connections to all available outputs.
                        aNewClient.OpenOutputConnections(myMessaging, anOutputChannelIds);

                        myConnectedClients[e.ResponseReceiverId] = aNewClient;
                    }
                }
            }

            protected override void OnResponseReceiverDisconnected(object sender, ResponseReceiverEventArgs e)
            {
                using (EneterTrace.Entering())
                {
                    using (ThreadLock.Lock(myClientConnectionLock))
                    {
                        TClient aClient;
                        myConnectedClients.TryGetValue(e.ResponseReceiverId, out aClient);
                        if (aClient != null)
                        {
                            aClient.CloseOutpuConnections();
                            myConnectedClients.Remove(e.ResponseReceiverId);
                        }
                    }
                }
            }

            private object myClientConnectionLock = new object();
            private Dictionary<string, TClient> myConnectedClients = new Dictionary<string, TClient>();
            private IMessagingSystemFactory myMessaging;
            private Func<IEnumerable<string>> myGetOutputChannelIds;

            protected override string TracedObject { get { return GetType().Name + ' '; } }
        }

        public DuplexDispatcher(IMessagingSystemFactory duplexOutputChannelMessagingSystem)
        {
            using (EneterTrace.Entering())
            {
                myMessagingSystemFactory = duplexOutputChannelMessagingSystem;
            }
        }


        public void AddDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myChannelManipulatorLock))
                {
                    myOutputChannelIds.Add(channelId);

                    // All clients open the new output connection to added channel id.
                    foreach (TInputChannelContext anInputChannelContext in myInputChannelContexts)
                    {
                        anInputChannelContext.OpenConnection(channelId);
                    }
                }
            }
        }

        public void RemoveDuplexOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myChannelManipulatorLock))
                {
                    myOutputChannelIds.Remove(channelId);
                    CloseOutputChannel(channelId);
                }
                
            }
        }

        public void RemoveAllDuplexOutputChannels()
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myChannelManipulatorLock))
                {
                    try
                    {
                        foreach (string aDuplexOutputChannelId in myOutputChannelIds)
                        {
                            CloseOutputChannel(aDuplexOutputChannelId);
                        }
                    }
                    finally
                    {
                        myOutputChannelIds.Clear();
                    }
                }
            }
        }

        public void AttachDuplexInputChannel(IDuplexInputChannel duplexInputChannel)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myChannelManipulatorLock))
                {
                    TInputChannelContext anInpuChannelContext = new TInputChannelContext(myMessagingSystemFactory, GetOutputChannelIds);
                    try
                    {
                        myInputChannelContexts.Add(anInpuChannelContext);
                        anInpuChannelContext.AttachDuplexInputChannel(duplexInputChannel);
                    }
                    catch (Exception err)
                    {
                        myInputChannelContexts.Remove(anInpuChannelContext);
                        EneterTrace.Error(TracedObject + "failed to attach duplex input channel.", err);
                    }
                }
            }
        }

        public void DetachDuplexInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myChannelManipulatorLock))
                {
                    for (int i = myInputChannelContexts.Count - 1; i >= 0; --i)
                    {
                        if (myInputChannelContexts[i].AttachedDuplexInputChannel.ChannelId == channelId)
                        {
                            myInputChannelContexts[i].DetachDuplexInputChannel();
                            myInputChannelContexts.RemoveAt(i);
                            break;
                        }
                    }
                }
            }
        }

        public void DetachDuplexInputChannel()
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myChannelManipulatorLock))
                {
                    foreach (TInputChannelContext anInputChannelContext in myInputChannelContexts)
                    {
                        anInputChannelContext.DetachDuplexInputChannel();
                    }

                    myInputChannelContexts.Clear();
                }
            }
        }

        public bool IsDuplexInputChannelAttached
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    using (ThreadLock.Lock(myChannelManipulatorLock))
                    {
                        return myInputChannelContexts.Any();
                    }
                }
            }
        }

        public IEnumerable<IDuplexInputChannel> AttachedDuplexInputChannels
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    using (ThreadLock.Lock(myChannelManipulatorLock))
                    {
                        List<IDuplexInputChannel> anInputChannels = new List<IDuplexInputChannel>();

                        foreach (TInputChannelContext anInputChannelContext in myInputChannelContexts)
                        {
                            anInputChannels.Add(anInputChannelContext.AttachedDuplexInputChannel);
                        }

                        return anInputChannels;
                    }
                }
            }
        }

        public string GetAssociatedResponseReceiverId(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myChannelManipulatorLock))
                {
                    foreach (TInputChannelContext anInputChannelContext in myInputChannelContexts)
                    {
                        string aClientResponseReceiverId = anInputChannelContext.GetAssociatedResponseReceiverId(responseReceiverId);
                        if (aClientResponseReceiverId != null)
                        {
                            return aClientResponseReceiverId;
                        }
                    }

                    return null;
                }
            }
        }

        private void CloseOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                foreach (TInputChannelContext anInputChannelContext in myInputChannelContexts)
                {
                    anInputChannelContext.CloseConnection(channelId);
                }
            }
        }

        private IEnumerable<string> GetOutputChannelIds()
        {
            using (ThreadLock.Lock(myChannelManipulatorLock))
            {
                return myOutputChannelIds.ToArray();
            }
        }


        private object myChannelManipulatorLock = new object();
        private IMessagingSystemFactory myMessagingSystemFactory;
        private HashSet<string> myOutputChannelIds = new HashSet<string>();
        private List<TInputChannelContext> myInputChannelContexts = new List<TInputChannelContext>();

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}
