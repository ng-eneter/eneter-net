/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using System.Collections.Generic;
using System.Linq;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.Utils.Collections;

namespace Eneter.Messaging.Infrastructure.Attachable
{
    /// <summary>
    /// The abstract class implementing the interface for attaching multiple input channels.
    /// The class also contains functionality to send (forward) messages via duplex output channels - the duplex input channel
    /// from the message is forwarded is remembered.
    /// The duplex output channels used for forwarding are not attached but dynamically created as they are needed.
    /// </summary>
    internal abstract class AttachableMultipleDuplexInputChannelsBase : IAttachableMultipleDuplexInputChannels
    {
        /// <summary>
        /// Represents the connection between the duplex input channel and the duplex output channel.
        /// So when the response from the duplex output channel is received it can be forwarded to attached the
        /// duplex input channel with the correct response receiver id.
        /// </summary>
        private class TConnection
        {
            public TConnection(string responseReceiverId, IDuplexOutputChannel duplexOutputChannel)
            {
                ResponseReceiverId = responseReceiverId;
                ConnectedDuplexOutputChannel = duplexOutputChannel;
            }

            public string ResponseReceiverId { get; private set; }
            public IDuplexOutputChannel ConnectedDuplexOutputChannel { get; private set; }
        }


        /// <summary>
        /// The context of the duplex input channel consists of the attached duplex input channel and
        /// it also can contain the list of duplex output channels used to forward the message.
        /// E.g. The DuplexDispatcher receives the message from the attached duplex input channel and then forwards
        /// it to all duplex output channels.
        /// E.g. The DuplexChannelWrapper receives the message from the attached duplex input channel then wrapps
        /// the message and sends it via the duplex output channel.
        /// </summary>
        private class TDuplexInputChannelContext
        {
            public TDuplexInputChannelContext(IDuplexInputChannel attachedDuplexInputChannel)
            {
                OpenConnections = new List<TConnection>();

                AttachedDuplexInputChannel = attachedDuplexInputChannel;
            }

            public IDuplexInputChannel AttachedDuplexInputChannel { get; private set; }
            public List<TConnection> OpenConnections { get; private set; }
        }

        public virtual void AttachDuplexInputChannel(IDuplexInputChannel duplexInputChannel)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myDuplexInputChannelContextManipulatorLock))
                {
                    Attach(duplexInputChannel);

                    try
                    {
                        duplexInputChannel.StartListening();
                    }
                    catch (Exception err)
                    {
                        duplexInputChannel.ResponseReceiverDisconnected -= OnDuplexInputChannelResponseReceiverDisconnected;
                        duplexInputChannel.MessageReceived -= OnMessageReceived;
                        myDuplexInputChannelContexts.RemoveWhere(x => x.AttachedDuplexInputChannel.ChannelId == duplexInputChannel.ChannelId);

                        EneterTrace.Error(TracedObject + "failed to attach the duplex input channel '" + duplexInputChannel.ChannelId + "'.", err);
                        throw;
                    }
                }
            }
        }

        public virtual void DetachDuplexInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myDuplexInputChannelContextManipulatorLock))
                {
                    // Get the context of the requested input channel.
                    TDuplexInputChannelContext aDuplexInputChannelContext = myDuplexInputChannelContexts.FirstOrDefault(x => x.AttachedDuplexInputChannel.ChannelId == channelId);
                    if (aDuplexInputChannelContext != null)
                    {
                        try
                        {
                            // Go via all connections with clients and close them.
                            CloseConnections(aDuplexInputChannelContext.OpenConnections);

                            // Stop listening to the duplex input channel.
                            aDuplexInputChannelContext.AttachedDuplexInputChannel.StopListening();
                        }
                        finally
                        {
                            aDuplexInputChannelContext.AttachedDuplexInputChannel.ResponseReceiverDisconnected -= OnDuplexInputChannelResponseReceiverDisconnected;
                            aDuplexInputChannelContext.AttachedDuplexInputChannel.MessageReceived -= OnMessageReceived;

                            myDuplexInputChannelContexts.RemoveWhere(x => x.AttachedDuplexInputChannel.ChannelId == aDuplexInputChannelContext.AttachedDuplexInputChannel.ChannelId);
                        }
                    }
                }
            }
        }

        public virtual void DetachDuplexInputChannel()
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myDuplexInputChannelContextManipulatorLock))
                {
                    foreach (TDuplexInputChannelContext aDuplexInputChannelContext in myDuplexInputChannelContexts)
                    {
                        // Go via all connections with clients and close them.
                        CloseConnections(aDuplexInputChannelContext.OpenConnections);

                        try
                        {
                            aDuplexInputChannelContext.AttachedDuplexInputChannel.StopListening();
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + "failed to correctly detach the duplex input channel '" + aDuplexInputChannelContext.AttachedDuplexInputChannel + "'.", err);
                        }

                        aDuplexInputChannelContext.AttachedDuplexInputChannel.ResponseReceiverDisconnected -= OnDuplexInputChannelResponseReceiverDisconnected;
                        aDuplexInputChannelContext.AttachedDuplexInputChannel.MessageReceived -= OnMessageReceived;
                    }

                    myDuplexInputChannelContexts.Clear();
                }
            }
        }

        public virtual bool IsDuplexInputChannelAttached
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    using (ThreadLock.Lock(myDuplexInputChannelContextManipulatorLock))
                    {
                        return myDuplexInputChannelContexts.Any();
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
                    // Note: Because of thread safety, create a new container to store the references.
                    List<IDuplexInputChannel> anAttachedChannels = new List<IDuplexInputChannel>();
                    using (ThreadLock.Lock(myDuplexInputChannelContextManipulatorLock))
                    {
                        foreach (TDuplexInputChannelContext aContextItem in myDuplexInputChannelContexts)
                        {
                            anAttachedChannels.Add(aContextItem.AttachedDuplexInputChannel);
                        }
                    }

                    return anAttachedChannels;
                }
            }
        }

        public string GetAssociatedResponseReceiverId(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myDuplexInputChannelContextManipulatorLock))
                {
                    // Go via all attached input channel contexts.
                    foreach (TDuplexInputChannelContext aContext in myDuplexInputChannelContexts)
                    {
                        // Check if some open connection for that input channel does not contain duplex output channel with
                        // passed responseReceiverId.
                        TConnection aConnection = aContext.OpenConnections.FirstOrDefault(x => x.ConnectedDuplexOutputChannel.ResponseReceiverId == responseReceiverId);
                        if (aConnection != null)
                        {
                            return aConnection.ResponseReceiverId;
                        }
                    }

                    return null;
                }
            }
        }

        protected void CloseDuplexOutputChannel(string duplexOutputChannelId)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myDuplexInputChannelContextManipulatorLock))
                {
                    foreach (TDuplexInputChannelContext aDuplexInputChannelContext in myDuplexInputChannelContexts)
                    {
                        IEnumerable<TConnection> aConnections = aDuplexInputChannelContext.OpenConnections.Where(x => x.ConnectedDuplexOutputChannel.ChannelId == duplexOutputChannelId);
                        CloseConnections(aConnections);
                        aDuplexInputChannelContext.OpenConnections.RemoveWhere(x => x.ConnectedDuplexOutputChannel.ChannelId == duplexOutputChannelId);
                    }
                }
            }
        }


        protected void SendMessage(string duplexInputChannelId, string duplexInputChannelResponseReceiverId, string duplexOutputChannelId, object message)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myDuplexInputChannelContextManipulatorLock))
                {
                    try
                    {
                        // Get (or create) the duplex output channel that will be used 
                        IDuplexOutputChannel aDuplexOutputChannel = GetAssociatedDuplexOutputChannel(duplexInputChannelId, duplexInputChannelResponseReceiverId, duplexOutputChannelId);
                        aDuplexOutputChannel.SendMessage(message);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + "failed to send the message to the duplex output channel '" + duplexOutputChannelId + "'.", err);
                        throw;
                    }
                }
            }
        }

        protected void SendResponseMessage(string duplexOutputChannelResponseReceiverId, object message)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myDuplexInputChannelContextManipulatorLock))
                {
                    TConnection anAssociatedConnection = null;
                    TDuplexInputChannelContext aDuplexInputChannelContext = myDuplexInputChannelContexts.FirstOrDefault(x =>
                        {
                            anAssociatedConnection = x.OpenConnections.FirstOrDefault(xx => xx.ConnectedDuplexOutputChannel.ResponseReceiverId == duplexOutputChannelResponseReceiverId);
                            return anAssociatedConnection != null;
                        });

                    if (aDuplexInputChannelContext == null)
                    {
                        string anError = TracedObject + "failed to send the response message because the duplex input channel associated with the response was not found.";
                        EneterTrace.Error(anError);
                        throw new InvalidOperationException(anError);
                    }

                    if (anAssociatedConnection == null)
                    {
                        string anError = TracedObject + "failed to send the response message because the duplex output channel with the given response receiver id was not found.";
                        EneterTrace.Error(anError);
                        throw new InvalidOperationException(anError);
                    }

                    try
                    {
                        aDuplexInputChannelContext.AttachedDuplexInputChannel.SendResponseMessage(anAssociatedConnection.ResponseReceiverId, message);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + "failed to send the response message for the response receiver '" + anAssociatedConnection.ResponseReceiverId + "' through the duplex input channel '" + aDuplexInputChannelContext.AttachedDuplexInputChannel.ChannelId + "'.", err);
                        throw;
                    }
                }
            }
        }

        private void Attach(IDuplexInputChannel duplexInputChannel)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myDuplexInputChannelContextManipulatorLock))
                {
                    if (duplexInputChannel == null)
                    {
                        string anError = TracedObject + "failed to attach the duplex input channel because the input parameter 'duplexInputChannel' is null.";
                        EneterTrace.Error(anError);
                        throw new ArgumentNullException(anError);
                    }

                    if (string.IsNullOrEmpty(duplexInputChannel.ChannelId))
                    {
                        string anError = TracedObject + "failed to attach the duplex input channel because the input parameter 'duplexInputChannel' has null or empty channel id.";
                        EneterTrace.Error(anError);
                        throw new ArgumentException(anError);
                    }

                    // If the channel with the same id is already attached then throw the exception.
                    if (myDuplexInputChannelContexts.Any(x => x.AttachedDuplexInputChannel.ChannelId == duplexInputChannel.ChannelId))
                    {
                        string anError = TracedObject + "failed to attach the duplex input channel '" + duplexInputChannel.ChannelId + "' because the duplex input channel with the same id is already attached.";
                        EneterTrace.Error(anError);
                        throw new InvalidOperationException(anError);
                    }

                    myDuplexInputChannelContexts.Add(new TDuplexInputChannelContext(duplexInputChannel));

                    // Start listening to the attached channel.
                    duplexInputChannel.ResponseReceiverDisconnected += OnDuplexInputChannelResponseReceiverDisconnected;
                    duplexInputChannel.MessageReceived += OnMessageReceived;
                }
            }
        }

        private IDuplexOutputChannel GetAssociatedDuplexOutputChannel(string duplexInputChannelId, string responseReceiverId, string duplexOutputChannelId)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myDuplexInputChannelContextManipulatorLock))
                {
                    // Find the requested input channel.
                    TDuplexInputChannelContext aDuplexInputChannelContext = myDuplexInputChannelContexts.FirstOrDefault(x => x.AttachedDuplexInputChannel.ChannelId == duplexInputChannelId);
                    if (aDuplexInputChannelContext == null)
                    {
                        string anError = TracedObject + "failed to return the duplex output channel associated with the duplex input channel '" + duplexInputChannelId + "' because the duplex input channel was not attached.";
                        EneterTrace.Error(anError);
                        throw new InvalidOperationException(anError);
                    }

                    // Try to find the requested output channel among open connections.
                    TConnection aConnection = aDuplexInputChannelContext.OpenConnections.FirstOrDefault(x => x.ResponseReceiverId == responseReceiverId && x.ConnectedDuplexOutputChannel.ChannelId == duplexOutputChannelId);
                    if (aConnection == null)
                    {
                        IDuplexOutputChannel aAssociatedDuplexOutputChannel = MessagingSystemFactory.CreateDuplexOutputChannel(duplexOutputChannelId);

                        try
                        {
                            aAssociatedDuplexOutputChannel.ResponseMessageReceived += OnResponseMessageReceived;
                            aAssociatedDuplexOutputChannel.OpenConnection();
                        }
                        catch (Exception err)
                        {
                            aAssociatedDuplexOutputChannel.ResponseMessageReceived -= OnResponseMessageReceived;

                            EneterTrace.Error(TracedObject + "failed to open connection for the duplex output channel '" + duplexOutputChannelId + "'.", err);
                            throw;
                        }


                        aConnection = new TConnection(responseReceiverId, aAssociatedDuplexOutputChannel);
                        aDuplexInputChannelContext.OpenConnections.Add(aConnection);
                    }

                    return aConnection.ConnectedDuplexOutputChannel;
                }
            }
        }


        /// <summary>
        /// Closes given connections with client duplex output channel.
        /// </summary>
        /// <param name="connections"></param>
        private void CloseConnections(IEnumerable<TConnection> connections)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myDuplexInputChannelContextManipulatorLock))
                {
                    foreach (TConnection aConnection in connections)
                    {
                        try
                        {
                            aConnection.ConnectedDuplexOutputChannel.CloseConnection();
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + "failed to close correctly the connection for the duplex output channel '" + aConnection.ConnectedDuplexOutputChannel.ChannelId + "'.", err);
                        }

                        aConnection.ConnectedDuplexOutputChannel.ResponseMessageReceived -= OnResponseMessageReceived;
                    }
                }
            }
        }

        private void OnDuplexInputChannelResponseReceiverDisconnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myDuplexInputChannelContextManipulatorLock))
                {
                    foreach (TDuplexInputChannelContext aDuplexInputChannelContext in myDuplexInputChannelContexts)
                    {
                        IEnumerable<TConnection> aConnections = aDuplexInputChannelContext.OpenConnections.Where(x => x.ResponseReceiverId == e.ResponseReceiverId);
                        CloseConnections(aConnections);
                        aDuplexInputChannelContext.OpenConnections.RemoveWhere(x => x.ResponseReceiverId == e.ResponseReceiverId);
                    }
                }
            }
        }


        protected abstract void OnMessageReceived(object sender, DuplexChannelMessageEventArgs e);
        protected abstract void OnResponseMessageReceived(object sender, DuplexChannelMessageEventArgs e);

        protected IMessagingSystemFactory MessagingSystemFactory { get; set; }

        private object myDuplexInputChannelContextManipulatorLock = new object();
        private List<TDuplexInputChannelContext> myDuplexInputChannelContexts = new List<TDuplexInputChannelContext>();


        protected virtual string TracedObject { get { return GetType().Name + ' '; } }
    }
}
