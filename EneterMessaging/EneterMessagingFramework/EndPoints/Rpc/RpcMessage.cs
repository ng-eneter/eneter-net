/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

using System;
using System.Runtime.Serialization;

namespace Eneter.Messaging.EndPoints.Rpc
{
#if !SILVERLIGHT
    [Serializable]
#endif
    [DataContract]
    public class RpcMessage
    {
        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public int Flag { get; set; }

        [DataMember]
        public string OperationName { get; set; }

        [DataMember]
        public object[] SerializedData { get; set; }

        [DataMember]
        public string Error { get; set; }
    }
}
