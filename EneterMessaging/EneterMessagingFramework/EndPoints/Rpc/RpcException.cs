/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/

using System;

namespace Eneter.Messaging.EndPoints.Rpc
{
    public class RpcException : Exception
    {
        internal RpcException(string message, string serviceExceptionType, string serviceExceptionDetails)
            :base(message)
        {
            ServiceExceptionType = serviceExceptionType;
            ServiceExceptionDetails = serviceExceptionDetails;
        }

        public string ServiceExceptionType { get; private set; }
        public string ServiceExceptionDetails { get; private set; }
    }
}
