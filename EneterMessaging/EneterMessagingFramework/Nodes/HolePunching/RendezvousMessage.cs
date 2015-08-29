using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eneter.Messaging.Nodes.HolePunching
{
    public enum ERendezvousMessage
    {
        RegisterRequest = 100,
        GetAddressRequest = 110,
        AddressResponse = 120
    }

    public class RendezvousMessage
    {
        public ERendezvousMessage MessageType { get; set; }
        public string MessageData { get; set; }
    }
}
