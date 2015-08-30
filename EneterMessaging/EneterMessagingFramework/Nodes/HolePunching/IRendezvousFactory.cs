/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/



namespace Eneter.Messaging.Nodes.HolePunching
{
    public interface IRendezvousFactory
    {
        IRendezvousService CreateRendezvousService();
        IRendezvousClient CreateRendezvousClient();
    }
}
