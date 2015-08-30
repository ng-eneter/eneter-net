/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/


using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.Nodes.HolePunching
{
    public class RendezvousFactory : IRendezvousFactory
    {
        public RendezvousFactory()
        {
            using (EneterTrace.Entering())
            {
                Serializer = new XmlStringSerializer();
            }
        }

        public IRendezvousService CreateRendezvousService()
        {
            return new RendezvousService(Serializer);
        }

        public IRendezvousClient CreateRendezvousClient()
        {
            return new RendezvousClient(Serializer);
        }

        public ISerializer Serializer { get; set; }
    }
}
