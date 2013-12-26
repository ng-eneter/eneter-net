/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

using System;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.EndPoints.Rpc
{
    public class RpcFactory : IRpcFactory
    {
        public RpcFactory()
            : this(new XmlStringSerializer())
        {
        }

        public RpcFactory(ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                Serializer = serializer;

                // Default timeout is set to infinite by default.
                RpcTimeout = TimeSpan.FromMilliseconds(-1);
            }
        }

        public IRpcClient<TServiceInterface> CreateClient<TServiceInterface>() where TServiceInterface : class
        {
            using (EneterTrace.Entering())
            {
                return new RpcClient<TServiceInterface>(Serializer, RpcTimeout);
            }
        }

        public IRpcService<TServiceInterface> CreateService<TServiceInterface>(TServiceInterface service) where TServiceInterface : class
        {
            using (EneterTrace.Entering())
            {
#if !COMPACT_FRAMEWORK20
                return new RpcService<TServiceInterface>(service, Serializer);
#else
                throw new NotSupportedException("RPC service is not supported in Compact Framework 2.0.");
#endif
            }
        }


        public ISerializer Serializer { get; set; }
        public TimeSpan RpcTimeout { get; set; }
    }
}
