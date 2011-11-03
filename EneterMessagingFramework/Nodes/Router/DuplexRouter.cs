/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System.Collections.Generic;
using System.Linq;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.Nodes.Router
{
    internal class DuplexRouter : AttachableMultipleDuplexInputChannelsBase, IDuplexRouter
    {
        private class TConnection
        {
            public TConnection(string duplexInputChannelId, string duplexOutputChannelId)
            {
                DuplexInputChannelId = duplexInputChannelId;
                DuplexOutputChannelId = duplexOutputChannelId;
            }

            public string DuplexInputChannelId { get; private set; }
            public string DuplexOutputChannelId { get; private set; }
        }

        public DuplexRouter(IMessagingSystemFactory duplexOutpuChannelsMessagingFactory)
        {
            using (EneterTrace.Entering())
            {
                MessagingSystemFactory = duplexOutpuChannelsMessagingFactory;
            }
        }

        public void AddConnection(string duplexInputChannelId, string duplexOutputChannelId)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnections)
                {
                    if (!myConnections.Any(x => x.DuplexInputChannelId == duplexInputChannelId && x.DuplexOutputChannelId == duplexOutputChannelId))
                    {
                        myConnections.Add(new TConnection(duplexInputChannelId, duplexOutputChannelId));
                    }
                }
            }
        }

        public void RemoveConnection(string duplexInputChannelId, string duplexOutputChannelId)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnections)
                {
                    IEnumerable<TConnection> aConnections = myConnections.Where(x => x.DuplexInputChannelId == duplexInputChannelId && x.DuplexOutputChannelId == duplexOutputChannelId);
                    foreach (TConnection aConnection in aConnections)
                    {
                        CloseDuplexOutputChannel(aConnection.DuplexOutputChannelId);
                    }

                    myConnections.RemoveWhere(x => x.DuplexInputChannelId == duplexInputChannelId && x.DuplexOutputChannelId == duplexOutputChannelId);
                }
            }
        }

        public void RemoveAllConnections()
        {
            using (EneterTrace.Entering())
            {
                lock (myConnections)
                {
                    foreach (TConnection aConnection in myConnections)
                    {
                        CloseDuplexOutputChannel(aConnection.DuplexOutputChannelId);
                    }

                    myConnections.Clear();
                }
            }
        }

        protected override void OnMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnections)
                {
                    IEnumerable<TConnection> aConfiguredConnections = myConnections.Where(x => x.DuplexInputChannelId == e.ChannelId);
                    foreach (TConnection aConnection in aConfiguredConnections)
                    {
                        SendMessage(aConnection.DuplexInputChannelId, e.ResponseReceiverId, aConnection.DuplexOutputChannelId, e.Message);
                    }
                }
            }
        }

        protected override void OnResponseMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    SendResponseMessage(e.ResponseReceiverId, e.Message);
                }
                catch
                {
                    // Nothing to do because the error was already traced.
                }
            }
        }



        private HashSet<TConnection> myConnections = new HashSet<TConnection>();

        protected override string TracedObject
        {
            get { return "DuplexRouter "; }
        }
    }
}
