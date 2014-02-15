/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using System.Collections.Generic;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.Nodes.ChannelWrapper
{
    internal sealed class DuplexChannelWrapper : AttachableDuplexOutputChannelBase, IDuplexChannelWrapper
    {
        private class TDuplexInputChannel
        {
            public TDuplexInputChannel(IDuplexInputChannel duplexInputChannel)
            {
                DuplexInputChannel = duplexInputChannel;
            }

            public IDuplexInputChannel DuplexInputChannel { get; private set; }
            public string ResponseReceiverId { get; set; }
        }

        public event EventHandler<DuplexChannelEventArgs> ConnectionOpened;
        public event EventHandler<DuplexChannelEventArgs> ConnectionClosed;

        public DuplexChannelWrapper(ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                mySerializer = serializer;
            }
        }


        public void AttachDuplexInputChannel(IDuplexInputChannel duplexInputChannel)
        {
            using (EneterTrace.Entering())
            {
                lock (myDuplexInputChannels)
                {
                    Attach(duplexInputChannel);

                    try
                    {

                        duplexInputChannel.StartListening();
                    }
                    catch (Exception err)
                    {
                        // Try to clean after the failure
                        try
                        {
                            DetachDuplexInputChannel(duplexInputChannel.ChannelId);
                        }
                        catch
                        {
                        }

                        string aMessage = TracedObject + "failed to start listening for '" + duplexInputChannel.ChannelId + "'.";
                        EneterTrace.Error(aMessage, err);
                        throw;
                    }
                }
            }
        }

        public void DetachDuplexInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                lock (myDuplexInputChannels)
                {
                    TDuplexInputChannel anInputChannel = null;
                    myDuplexInputChannels.TryGetValue(channelId, out anInputChannel);
                    if (anInputChannel != null)
                    {
                        anInputChannel.DuplexInputChannel.StopListening();
                        anInputChannel.DuplexInputChannel.MessageReceived -= OnMessageReceived;
                    }

                    myDuplexInputChannels.Remove(channelId);
                }
            }
        }

        public void DetachDuplexInputChannel()
        {
            using (EneterTrace.Entering())
            {
                lock (myDuplexInputChannels)
                {
                    foreach (TDuplexInputChannel anInputChannel in myDuplexInputChannels.Values)
                    {
                        anInputChannel.DuplexInputChannel.StopListening();
                        anInputChannel.DuplexInputChannel.MessageReceived -= OnMessageReceived;
                    }

                    myDuplexInputChannels.Clear();
                }
            }
        }

        public bool IsDuplexInputChannelAttached
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    lock (myDuplexInputChannels)
                    {
                        return myDuplexInputChannels.Count > 0;
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
                    lock (myDuplexInputChannels)
                    {
                        foreach (TDuplexInputChannel aDuplexInputChannelItem in myDuplexInputChannels.Values)
                        {
                            anAttachedChannels.Add(aDuplexInputChannelItem.DuplexInputChannel);
                        }
                    }

                    return anAttachedChannels;
                }
            }
        }

        /// <summary>
        /// The method is called when a message from the attached duplex input channel is received.
        /// The received message is wrapped and sent to the duplex output channel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (!IsDuplexOutputChannelAttached)
                {
                    EneterTrace.Error(TracedObject + "is not attached to the duplex output channel.");
                    return;
                }

                try
                {
                    lock (myDuplexInputChannels)
                    {
                        myDuplexInputChannels[e.ChannelId].ResponseReceiverId = e.ResponseReceiverId;
                    }

                    object aMessage = DataWrapper.Wrap(e.ChannelId, e.Message, mySerializer);
                    AttachedDuplexOutputChannel.SendMessage(aMessage);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to send the message to the duplex output channel '" + e.ChannelId + "'.", err);
                }
            }
        }

        protected override void OnConnectionOpened(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                Notify(ConnectionOpened, e);
            }
        }

        protected override void OnConnectionClosed(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                Notify(ConnectionClosed, e);
            }
        }

        /// <summary>
        /// The method is called when a reponse message is received from the duplex output channel.
        /// The received response is unwrapped and sent as a response to the matching duplex input channel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnResponseMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    WrappedData aWrappedData = DataWrapper.Unwrap(e.Message, mySerializer);

                    // WrappedData.AddedData represents the channel id.
                    // Therefore if everything is ok then it must be string.
                    if (aWrappedData.AddedData is string)
                    {
                        // Get the output channel according to the channel id.
                        TDuplexInputChannel aDuplexInputChannel = null;

                        lock (myDuplexInputChannels)
                        {
                            myDuplexInputChannels.TryGetValue((string)aWrappedData.AddedData, out aDuplexInputChannel);
                        }

                        if (aDuplexInputChannel != null)
                        {
                            aDuplexInputChannel.DuplexInputChannel.SendResponseMessage(aDuplexInputChannel.ResponseReceiverId, aWrappedData.OriginalData);
                        }
                        else
                        {
                            EneterTrace.Warning(TracedObject + "could not send the response message to the duplex input channel '" + (string)aWrappedData.AddedData + "' because the channel is not attached to the unwrapper.");
                        }
                    }
                    else
                    {
                        EneterTrace.Error(TracedObject + "detected that the unwrapped message does not contain the channel id as the string type.");
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to process the response message.", err);
                }
            }
        }

        private void Attach(IDuplexInputChannel duplexInputChannel)
        {
            using (EneterTrace.Entering())
            {
                lock (myDuplexInputChannels)
                {
                    if (duplexInputChannel == null)
                    {
                        string aMessage = TracedObject + "failed to attach duplex input channel because the input parameter 'duplexInputChannel' is null.";
                        EneterTrace.Error(aMessage);
                        throw new ArgumentNullException(aMessage);
                    }

                    if (string.IsNullOrEmpty(duplexInputChannel.ChannelId))
                    {
                        string aMessage = TracedObject + "failed to attach duplex input channel because the input parameter 'duplexInputChannel' has empty or null channel id.";
                        EneterTrace.Error(aMessage);
                        throw new ArgumentException(aMessage);
                    }

                    if (myDuplexInputChannels.ContainsKey(duplexInputChannel.ChannelId))
                    {
                        string anErrorMessage = TracedObject + "failed to attach duplex input channel because the channel with id '" + duplexInputChannel.ChannelId + "' is already attached.";
                        EneterTrace.Error(anErrorMessage);
                        throw new InvalidOperationException(anErrorMessage);
                    }

                    myDuplexInputChannels[duplexInputChannel.ChannelId] = new TDuplexInputChannel(duplexInputChannel);

                    duplexInputChannel.MessageReceived += OnMessageReceived;
                }
            }
        }

        private void Notify(EventHandler<DuplexChannelEventArgs> handler, DuplexChannelEventArgs e)
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
                        EneterTrace.Error(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }

        private Dictionary<string, TDuplexInputChannel> myDuplexInputChannels = new Dictionary<string, TDuplexInputChannel>();
        private ISerializer mySerializer;

        protected override string TracedObject
        {
            get
            { 
                string aDuplexOutputChannelId = (AttachedDuplexOutputChannel != null) ? AttachedDuplexOutputChannel.ChannelId : "";
                return GetType().Name + " '" + aDuplexOutputChannelId + "' "; 
            } 
        }
    }
}
