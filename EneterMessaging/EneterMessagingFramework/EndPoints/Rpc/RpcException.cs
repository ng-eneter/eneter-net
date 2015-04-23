/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/

using System;

namespace Eneter.Messaging.EndPoints.Rpc
{
    public class RpcException : InvalidOperationException
    {
        public RpcException(string message, string serviceExceptionType, string details)
            :base(message)
        {
            ServiceExceptionType = serviceExceptionType;
            ServiceExceptionDetails = details;
        }

        public string ServiceExceptionType { get; private set; }
        public string ServiceExceptionDetails { get; private set; }
    }
}
