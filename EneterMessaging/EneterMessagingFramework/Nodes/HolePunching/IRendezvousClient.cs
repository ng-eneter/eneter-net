using Eneter.Messaging.Infrastructure.Attachable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eneter.Messaging.Nodes.HolePunching
{
    public interface IRendezvousClient : IAttachableDuplexOutputChannel
    {
        string Register(string rendezvousId);
        string GetAddress(string rendezvousId);
    }
}
