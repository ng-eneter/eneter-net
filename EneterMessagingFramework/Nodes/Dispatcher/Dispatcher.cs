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

namespace Eneter.Messaging.Nodes.Dispatcher
{
    internal class Dispatcher : IDispatcher
    {
        public void AttachOutputChannel(IOutputChannel outputChannel)
        {
            using (EneterTrace.Entering())
            {
                lock (myOutputChannels)
                {
                    IOutputChannel anOutputChannel = myOutputChannels.FirstOrDefault(x => x.ChannelId == outputChannel.ChannelId);
                    if (anOutputChannel != null)
                    {
                        string anErrorMessage = TracedObject + "cannot attach the output channel because the output channel with the id '" + outputChannel.ChannelId + "' is already attached.";
                        EneterTrace.Error(anErrorMessage);
                        throw new InvalidOperationException(anErrorMessage);
                    }

                    myOutputChannels.Add(outputChannel);
                }
            }
        }

        public void DetachOutputChannel(string outputChannelId)
        {
            using (EneterTrace.Entering())
            {
                lock (myOutputChannels)
                {
                    myOutputChannels.RemoveWhere(x => x.ChannelId == outputChannelId);
                }
            }
        }

        public void DetachOutputChannel()
        {
            using (EneterTrace.Entering())
            {
                lock (myOutputChannels)
                {
                    myOutputChannels.Clear();
                }
            }
        }

        public bool IsOutputChannelAttached
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    lock (myOutputChannels)
                    {
                        return myOutputChannels.Count > 0;
                    }
                }
            }
        }

        public IEnumerable<IOutputChannel> AttachedOutputChannels
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    lock (myOutputChannels)
                    {
                        return myOutputChannels.ToList();
                    }
                }
            }
        }

        public void AttachInputChannel(IInputChannel inputChannel)
        {
            using (EneterTrace.Entering())
            {
                lock (myInputChannels)
                {
                    IInputChannel anInputChannel = myInputChannels.FirstOrDefault(x => x.ChannelId == inputChannel.ChannelId);
                    if (anInputChannel != null)
                    {
                        string anErrorMessage = TracedObject + "cannot attach the input channel because the input channel with the id '" + inputChannel.ChannelId + "' is already attached.";
                        EneterTrace.Error(anErrorMessage);
                        throw new InvalidOperationException(anErrorMessage);
                    }

                    myInputChannels.Add(inputChannel);
                    inputChannel.MessageReceived += OnMessageReceived;

                    try
                    {
                        inputChannel.StartListening();
                    }
                    catch (Exception err)
                    {
                        inputChannel.MessageReceived -= OnMessageReceived;
                        myInputChannels.Remove(anInputChannel);

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
                    IInputChannel anInputChannel = myInputChannels.FirstOrDefault(x => x.ChannelId == inputChannelId);
                    if (anInputChannel != null)
                    {
                        anInputChannel.StopListening();
                        anInputChannel.MessageReceived -= OnMessageReceived;

                        myInputChannels.Remove(anInputChannel);
                    }
                }
            }
        }

        public void DetachInputChannel()
        {
            using (EneterTrace.Entering())
            {
                lock (myInputChannels)
                {
                    foreach (IInputChannel anInputChannel in myInputChannels)
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
                        return myInputChannels.ToList();
                    }
                }
            }
        }

        /// <summary>
        /// The method is called when a message is received from an input channel.
        /// The message is then sent to all attached output channels.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnMessageReceived(object sender, ChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                lock (myOutputChannels)
                {
                    // Forward the message to all output channels
                    foreach (IOutputChannel anOuputChannel in myOutputChannels)
                    {
                        try
                        {
                            anOuputChannel.SendMessage(e.Message);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Error(TracedObject + "failed to send the message to the output channel '" + anOuputChannel.ChannelId + "'.", err);
                        }
                    }
                }
            }
        }

        private HashSet<IInputChannel> myInputChannels = new HashSet<IInputChannel>();
        private HashSet<IOutputChannel> myOutputChannels = new HashSet<IOutputChannel>();


        private string TracedObject
        {
            get
            {
                return "The Dispatcher ";
            }
        }
    }
}
