/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/

using System;
using System.Runtime.Serialization;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
#if !SILVERLIGHT
    [Serializable]
#endif
    [DataContract]
    public class MultiTypedMessage
    {
        [DataMember]
        public string TypeName { get; set; }

        [DataMember]
        public object MessageData { get; set; }
    }
}
