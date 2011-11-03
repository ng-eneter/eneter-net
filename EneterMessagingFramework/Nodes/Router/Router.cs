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

namespace Eneter.Messaging.Nodes.Router
{
    internal class Router : IRouter
    {
        private class TConnection
        {
            public TConnection(IInputChannel inputChannel, IOutputChannel outputChannel)
            {
                InputChannel = inputChannel;
                OutputChannel = outputChannel;
            }

            public IInputChannel InputChannel { get; private set; }
            public IOutputChannel OutputChannel { get; private set; }
        }

        /// <summary>
        /// Creates the connection between the input channel and the output channel.
        /// One input channel can have configured connection with more output channels.
        /// When a message is received from the input channel the message is then forwarded to all
        /// output channels that are connected with the input channel.
        /// </summary>
        /// <param name="inputChannelId"></param>
        /// <param name="outputChannelId"></param>
        public void AddConnection(string inputChannelId, string outputChannelId)
        {
            using (EneterTrace.Entering())
            {
                lock (this)
                {
                    IInputChannel anInputChannel = null;
                    myInputChannels.TryGetValue(inputChannelId, out anInputChannel);
                    if (anInputChannel == null)
                    {
                        string aMessage = TracedObject + "failed to create the connection because the input channel '" + inputChannelId + "' is not attached to the router.";
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    IOutputChannel anOutputChannel = null;
                    myOutputChannels.TryGetValue(outputChannelId, out anOutputChannel);
                    if (anInputChannel == null)
                    {
                        string aMessage = TracedObject + "failed to create the connection because the output channel '" + outputChannelId + "' is not attached to the router.";
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    if (myConnections.Count(x => x.InputChannel.ChannelId == inputChannelId && x.OutputChannel.ChannelId == outputChannelId) == 0)
                    {
                        myConnections.Add(new TConnection(anInputChannel, anOutputChannel));
                    }
                }
            }
        }

        public void RemoveConnection(string inputChannelId, string outputChannelId)
        {
            using (EneterTrace.Entering())
            {
                lock (this)
                {
                    myConnections.RemoveWhere(x => x.InputChannel.ChannelId == inputChannelId && x.OutputChannel.ChannelId == outputChannelId);
                }
            }
        }

        public void RemoveInputChannelConnections(string inputChannelId)
        {
            using (EneterTrace.Entering())
            {
                lock (this)
                {
                    myConnections.RemoveWhere(x => x.InputChannel.ChannelId == inputChannelId);
                }
            }
        }

        public void RemoveOutputChannelConnections(string outputChannelId)
        {
            using (EneterTrace.Entering())
            {
                lock (this)
                {
                    myConnections.RemoveWhere(x => x.InputChannel.ChannelId == outputChannelId);
                }
            }
        }


        public void AttachInputChannel(IInputChannel inputChannel)
        {
            using (EneterTrace.Entering())
            {
                lock (this)
                {
                    if (myInputChannels.ContainsKey(inputChannel.ChannelId))
                    {
                        string anErrorMessage = TracedObject + "cannot attach the input channel because the input channel with the id '" + inputChannel.ChannelId + "' is already attached.";
                        EneterTrace.Error(anErrorMessage);
                        throw new InvalidOperationException(anErrorMessage);
                    }

                    myInputChannels[inputChannel.ChannelId] = inputChannel;

                    inputChannel.MessageReceived += OnChannelMessageReceived;
                    inputChannel.StartListening();
                }
            }
        }

        public void DetachInputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                lock (this)
                {
                    IInputChannel anInputChannel = null;
                    myInputChannels.TryGetValue(channelId, out anInputChannel);
                    if (anInputChannel != null)
                    {
                        anInputChannel.StopListening();
                        anInputChannel.MessageReceived -= OnChannelMessageReceived;
                    }

                    RemoveInputChannelConnections(channelId);

                    myInputChannels.Remove(channelId);
                }
            }
        }

        public void DetachInputChannel()
        {
            using (EneterTrace.Entering())
            {
                lock (this)
                {
                    foreach (IInputChannel anInputChannel in myInputChannels.Values)
                    {
                        anInputChannel.StopListening();
                        anInputChannel.MessageReceived -= OnChannelMessageReceived;
                    }

                    myConnections.Clear();
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
                    lock (this)
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
                    lock (this)
                    {
                        return myInputChannels.Values.ToList();
                    }
                }
            }
        }

        public void AttachOutputChannel(IOutputChannel outputChannel)
        {
            using (EneterTrace.Entering())
            {
                lock (this)
                {
                    if (myOutputChannels.ContainsKey(outputChannel.ChannelId))
                    {
                        string anErrorMessage = TracedObject + "cannot attach the output channel because the output channel with the id '" + outputChannel.ChannelId + "' is already attached.";
                        EneterTrace.Error(anErrorMessage);
                        throw new InvalidOperationException(anErrorMessage);
                    }

                    myOutputChannels[outputChannel.ChannelId] = outputChannel;
                }
            }
        }

        public void DetachOutputChannel(string outputChannelId)
        {
            using (EneterTrace.Entering())
            {
                lock (this)
                {
                    RemoveOutputChannelConnections(outputChannelId);
                    myOutputChannels.Remove(outputChannelId);
                }
            }
        }

        public void DetachOutputChannel()
        {
            using (EneterTrace.Entering())
            {
                lock (this)
                {
                    myConnections.Clear();
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
                    lock (this)
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
                    lock (this)
                    {
                        return myOutputChannels.Values.ToList();
                    }
                }
            }
        }

        /// <summary>
        /// The method is called wehen the message is received.
        /// The message is then forwarded to all created connections.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnChannelMessageReceived(object sender, ChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                lock (this)
                {
                    IEnumerable<TConnection> aConnections = myConnections.Where(x => x.InputChannel.ChannelId == e.ChannelId);
                    foreach (TConnection aConnection in aConnections)
                    {
                        try
                        {
                            aConnection.OutputChannel.SendMessage(e.Message);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Error(TracedObject + "failed to send the message to the output channel '" + aConnection.OutputChannel.ChannelId + "'.", err);
                        }
                    }
                }
            }
        }

        private Dictionary<string, IInputChannel> myInputChannels = new Dictionary<string, IInputChannel>();
        private Dictionary<string, IOutputChannel> myOutputChannels = new Dictionary<string, IOutputChannel>();
        private HashSet<TConnection> myConnections = new HashSet<TConnection>();


        private string TracedObject
        {
            get
            {
                return "The Router ";
            }
        }
    }
}
