/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/

using System;

namespace Eneter.Messaging.EndPoints.Rpc
{
    /// <summary>
    /// Exception thrown if an RPC call fails on the service side.
    /// </summary>
    /// <remarks>
    /// E.g. in case the service method throws an exception it is transfered to the client.
    /// When the client receives the exception from the service it creates RpcException and stores there all details
    /// about original service exception. The RpcException is then thrown and can be processed by the client.  
    /// </remarks>
    public sealed class RpcException : Exception
    {
        internal RpcException(string message, string serviceExceptionType, string serviceExceptionDetails)
            :base(message)
        {
            ServiceExceptionType = serviceExceptionType;
            ServiceExceptionDetails = serviceExceptionDetails;
        }

        /// <summary>
        /// Gets name of the exception type thrown in the service.
        /// </summary>
        public string ServiceExceptionType { get; private set; }

        /// <summary>
        /// Gets service exception details including callstack.
        /// </summary>
        public string ServiceExceptionDetails { get; private set; }
    }
}
