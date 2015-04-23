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
    /// <summary>
    /// Internal message used for the communication between RpcClient and RpcService.
    /// </summary>
#if !SILVERLIGHT
    [Serializable]
#endif
    [DataContract]
    public class RpcMessage
    {
        /// <summary>
        /// Identifies the request on the client side.
        /// </summary>
        [DataMember]
        public int Id { get; set; }

        /// <summary>
        /// Identifies the type of the request/response message.
        /// </summary>
        /// <remarks>
        /// e.g. if it is InvokeMethod, MethodResponse, SubscribeEvent, UnsubscribeEvent, RaiseEvent.
        /// </remarks>
        [DataMember]
        public int Flag { get; set; }

        /// <summary>
        /// The name of the operation that shall be performed.
        /// </summary>
        /// <remarks>
        /// e.g. in case of InvokeMethod it specifies which method shall be invoked.
        /// </remarks>
        [DataMember]
        public string OperationName { get; set; }

        /// <summary>
        /// Message data.
        /// </summary>
        /// <remarks>
        /// e.g. in case of InvokeMethod it contains input parameters data.
        /// </remarks>
        [DataMember]
        public object[] SerializedData { get; set; }

        /// <summary>
        /// If an error occurred in the service.
        /// </summary>
        [DataMember]
        public string Error { get; set; }

        [DataMember]
        public string ErrorMessage { get; set; }

        [DataMember]
        public string ErrorDetails { get; set; }
    }
}
