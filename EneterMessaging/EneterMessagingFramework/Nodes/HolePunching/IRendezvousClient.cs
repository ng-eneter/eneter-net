/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/


using Eneter.Messaging.Infrastructure.Attachable;

namespace Eneter.Messaging.Nodes.HolePunching
{
    public interface IRendezvousClient : IAttachableDuplexOutputChannel
    {
        string Register(string rendezvousId);
        string GetEndPoint(string rendezvousId);
    }
}
