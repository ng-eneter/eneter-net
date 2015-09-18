/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/


using System;
using System.Runtime.Serialization;

namespace Eneter.Messaging.Nodes.HolePunching
{
    public enum ERendezvousMessage
    {
        RegisterRequest = 100,
        RegisterResponse = 110,
        GetAddressRequest = 120,
        GetAddressResponse = 130,
        Drill = 140
    }

#if !SILVERLIGHT
    [Serializable]
#endif
    [DataContract]
    public class RendezvousMessage
    {
        [DataMember]
        public ERendezvousMessage MessageType { get; set; }

        [DataMember]
        public string MessageData { get; set; }
    }
}
