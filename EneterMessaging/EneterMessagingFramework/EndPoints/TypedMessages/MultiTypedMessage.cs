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
    /// <summary>
    /// Internal message used for the communication between multi-typed message sender and receiver.
    /// </summary>
#if !SILVERLIGHT
    [Serializable]
#endif
    [DataContract]
    public class MultiTypedMessage
    {
        /// <summary>
        /// Name of the message type (without namespace).
        /// </summary>
        /// <remarks>
        /// In order to ensure better portability between .NET and Java the type name does not include the whole namespace
        /// but only the name.
        /// </remarks>
        [DataMember]
        public string TypeName { get; set; }

        /// <summary>
        /// Serialized message.
        /// </summary>
        [DataMember]
        public object MessageData { get; set; }
    }
}
