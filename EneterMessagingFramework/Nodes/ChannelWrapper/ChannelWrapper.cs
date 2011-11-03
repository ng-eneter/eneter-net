/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using System.Linq;
using System.Collections.Generic;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.DataProcessing.Wrapping;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.Nodes.ChannelWrapper
{
    internal class ChannelWrapper : AttachableOutputChannelBase, IChannelWrapper
    {
        public ChannelWrapper(ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                mySerializer = serializer;
            }
        }

        public void AttachInputChannel(IInputChannel inputChannel)
        {
            using (EneterTrace.Entering())
            {
                lock (myInputChannels)
                {
                    if (myInputChannels.ContainsKey(inputChannel.ChannelId))
                    {
                        string anErrorMessage = TracedObject + "cannot attach the input channel because the input channel with the id '" + inputChannel.ChannelId + "' is already attached.";
                        EneterTrace.Error(anErrorMessage);
                        throw new InvalidOperationException(anErrorMessage);
                    }

                    myInputChannels[inputChannel.ChannelId] = inputChannel;
                    inputChannel.MessageReceived += OnMessageReceived;

                    try
                    {
                        inputChannel.StartListening();
                    }
                    catch (Exception err)
                    {
                        inputChannel.MessageReceived -= OnMessageReceived;
                        myInputChannels.Remove(inputChannel.ChannelId);

                        EneterTrace.Error(TracedObject + "failed to attach the input channel '" + inputChannel.ChannelId + "'.", err);

                        throw;
                    }
                }
            }
        }

        public void DetachInputChannel(string inputChannelId)
        {
            using (EneterTrace.Entering())
            {
                lock (myInputChannels)
                {
                    IInputChannel anInputChannel = null;
                    myInputChannels.TryGetValue(inputChannelId, out anInputChannel);
                    if (anInputChannel != null)
                    {
                        anInputChannel.StopListening();
                        anInputChannel.MessageReceived -= OnMessageReceived;
                    }

                    myInputChannels.Remove(inputChannelId);
                }
            }
        }

        public void DetachInputChannel()
        {
            using (EneterTrace.Entering())
            {
                lock (myInputChannels)
                {
                    foreach (IInputChannel anInputChannel in myInputChannels.Values)
                    {
                        anInputChannel.StopListening();
                        anInputChannel.MessageReceived -= OnMessageReceived;
                    }

                    myInputChannels.Clear();
                }
            }
        }

        public bool IsInputChannelAttached
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    lock (myInputChannels)
                    {
                        return myInputChannels.Count > 0;
                    }
                }
            }
        }

        public IEnumerable<IInputChannel> AttachedInputChannels
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    lock (myInputChannels)
                    {
                        return myInputChannels.Values.ToList();
                    }
                }
            }
        }

        /// <summary>
        /// The method is called when the channel wrapper receives a message from one of input channels.
        /// It wrapps the message and sends it to the output channel.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMessageReceived(object sender, ChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    object aMessage = DataWrapper.Wrap(e.ChannelId, e.Message, mySerializer);
                    AttachedOutputChannel.SendMessage(aMessage);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to send the wrapped message.", err);
                }
            }
        }


        private Dictionary<string, IInputChannel> myInputChannels = new Dictionary<string, IInputChannel>();

        private ISerializer mySerializer;


        private string TracedObject
        {
            get
            {
                string aDuplexOutputChannelId = (AttachedOutputChannel != null) ? AttachedOutputChannel.ChannelId : "";
                return "The ChannelWrapper attached to the output channel '" + aDuplexOutputChannelId + "' ";
            }
        }
    }
}
