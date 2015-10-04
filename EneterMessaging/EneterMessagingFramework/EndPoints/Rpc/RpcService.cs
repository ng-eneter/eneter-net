/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

#if !COMPACT_FRAMEWORK20

using System;
using System.Collections.Generic;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.EndPoints.Rpc
{
    internal class RpcService<TServiceInterface> : AttachableDuplexInputChannelBase, IRpcService<TServiceInterface>
        where TServiceInterface : class
    {
        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;
        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;


        public RpcService(TServiceInterface singletonService, ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                if (serializer == null)
                {
                    string anError = "Input parameter serializer is null.";
                    EneterTrace.Error(anError);
                    throw new ArgumentNullException(anError);
                }

                ServiceInterfaceChecker.Check<TServiceInterface>();
                mySingletonService = new ServiceStub<TServiceInterface>(singletonService, serializer);
            }
        }

        public RpcService(Func<TServiceInterface> serviceFactoryMethod, ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                if (serializer == null)
                {
                    string anError = "Input parameter serializer is null.";
                    EneterTrace.Error(anError);
                    throw new ArgumentNullException(anError);
                }

                ServiceInterfaceChecker.Check<TServiceInterface>();

                myServiceFactoryMethod = serviceFactoryMethod;

                mySerializer = serializer;
            }
        }

        public override void AttachDuplexInputChannel(IDuplexInputChannel duplexInputChannel)
        {
            using (EneterTrace.Entering())
            {
                // If this is singleton service mode.
                if (mySingletonService != null)
                {
                    mySingletonService.AttachInputChannel(duplexInputChannel);
                }

                base.AttachDuplexInputChannel(duplexInputChannel);
            }
        }

        public override void DetachDuplexInputChannel()
        {
            using (EneterTrace.Entering())
            {
                base.DetachDuplexInputChannel();

                // If this is singleton service mode.
                if (mySingletonService != null)
                {
                    mySingletonService.DetachInputChannel();
                }
                else
                {
                    // If per client mode then detach all service stubs.
                    using (ThreadLock.Lock(myPerConnectionServices))
                    {
                        foreach (KeyValuePair<string, ServiceStub<TServiceInterface>> aServiceStub in myPerConnectionServices)
                        {
                            aServiceStub.Value.UnsubscribeClientFromEvents(aServiceStub.Key);
                            aServiceStub.Value.DetachInputChannel();
                        }
                    }
                }
            }
        }

        protected override void OnResponseReceiverConnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                // If per client mode then create service stub for connected client.
                if (myServiceFactoryMethod != null)
                {
                    TServiceInterface aServiceInstanceForThisClient = myServiceFactoryMethod();
                    ServiceStub<TServiceInterface> aServiceStub = new ServiceStub<TServiceInterface>(aServiceInstanceForThisClient, mySerializer);
                    aServiceStub.AttachInputChannel(AttachedDuplexInputChannel);

                    using (ThreadLock.Lock(myPerConnectionServices))
                    {
                        myPerConnectionServices[e.ResponseReceiverId] = aServiceStub;
                    }
                }

                if (ResponseReceiverConnected != null)
                {
                    ResponseReceiverConnected(this, e);
                }
            }
        }

        protected override void OnResponseReceiverDisconnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (mySingletonService != null)
                {
                    mySingletonService.UnsubscribeClientFromEvents(e.ResponseReceiverId);
                }
                else
                {
                    // If per client mode then remove service stub for the disconnected client.
                    using (ThreadLock.Lock(myPerConnectionServices))
                    {
                        // Unsubscribe disconnected client from all events.
                        ServiceStub<TServiceInterface> aServiceStub;
                        myPerConnectionServices.TryGetValue(e.ResponseReceiverId, out aServiceStub);
                        if (aServiceStub != null)
                        {
                            aServiceStub.UnsubscribeClientFromEvents(e.ResponseReceiverId);
                            aServiceStub.DetachInputChannel();
                            myPerConnectionServices.Remove(e.ResponseReceiverId);
                        }
                    }
                }

                if (ResponseReceiverDisconnected != null)
                {
                    ResponseReceiverDisconnected(this, e);
                }
            }
        }

        protected override void OnRequestMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (mySingletonService != null)
                {
                    mySingletonService.ProcessRemoteRequest(e);
                }
                else
                {
                    // If per client mode then find the service stub associated with the client and execute the
                    // remote request.
                    using (ThreadLock.Lock(myPerConnectionServices))
                    {
                        ServiceStub<TServiceInterface> aServiceStub;
                        myPerConnectionServices.TryGetValue(e.ResponseReceiverId, out aServiceStub);
                        if (aServiceStub != null)
                        {
                            aServiceStub.ProcessRemoteRequest(e);
                        }
                    }
                }
            }
        }

        private ServiceStub<TServiceInterface> mySingletonService;
        private Dictionary<string, ServiceStub<TServiceInterface>> myPerConnectionServices = new Dictionary<string, ServiceStub<TServiceInterface>>();
        private ISerializer mySerializer;
        private Func<TServiceInterface> myServiceFactoryMethod;

        protected override string TracedObject { get { return GetType().Name + " "; } }
    }
}

#endif