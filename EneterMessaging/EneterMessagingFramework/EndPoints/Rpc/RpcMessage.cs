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
    [Serializable]
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
        /// e.g. if it is InvokeMethod, SubscribeEvent, UnsubscribeEvent, RaiseEvent or Response.
        /// </remarks>
        [DataMember]
        public ERpcRequest Request { get; set; }

        /// <summary>
        /// The name of the method or event which shall be performed.
        /// </summary>
        /// <remarks>
        /// e.g. in case of InvokeMethod it specifies which method shall be invoked.
        /// </remarks>
        [DataMember]
        public string OperationName { get; set; }

        /// <summary>
        /// Serialized input parameters.
        /// </summary>
        [DataMember]
        public object[] SerializedParams { get; set; }

        /// <summary>
        /// Serialized return value.
        /// </summary>
        /// <remarks>
        /// If it is method returning void then the return value is null.
        /// </remarks>
        [DataMember]
        public object SerializedReturn { get; set; }

        /// <summary>
        /// If an error occurred in the service.
        /// </summary>
        [DataMember]
        public string ErrorType { get; set; }

        /// <summary>
        /// Exception message from the service.
        /// </summary>
        [DataMember]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Exception details from the service.
        /// </summary>
        [DataMember]
        public string ErrorDetails { get; set; }
    }
}
