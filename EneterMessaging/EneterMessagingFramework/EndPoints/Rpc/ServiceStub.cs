﻿/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

#if !COMPACT_FRAMEWORK20

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.EndPoints.Rpc
{
    internal class ServiceStub<TServiceInterface>
    {
        // Maintains events and subscribed clients.
        private class EventContext
        {
            public EventContext(TServiceInterface service, EventInfo eventInfo, Delegate handler)
            {
                myService = service;
                EventInfo = eventInfo;
                myHandler = handler;
                SubscribedClients = new HashSet<string>();
            }

            // Subscribes anonymous event handler in the service.
            // When an event occurs the anonymous event handler forwards the event to subscribed remote clients.
            public void Subscribe()
            {
                EventInfo.AddEventHandler(myService, myHandler);
            }

            public void Unsubscribe()
            {
                EventInfo.RemoveEventHandler(myService, myHandler);
            }

            public EventInfo EventInfo { get; private set; }
            public HashSet<string> SubscribedClients { get; private set; }


            private TServiceInterface myService;
            private Delegate myHandler;
        }

        // Maintains info about a service method.
        private class ServiceMethod
        {
            public ServiceMethod(MethodInfo methodInfo)
            {
                Method = methodInfo;
                InputParameterTypes = methodInfo.GetParameters().Select(x => x.ParameterType).ToArray();
            }

            public MethodInfo Method { get; private set; }
            public Type[] InputParameterTypes { get; private set; }
        }


        public ServiceStub(TServiceInterface service, ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                myService = service;
                mySerializer = serializer;

                foreach (MethodInfo aMethod in typeof(TServiceInterface).GetMethods())
                {
                    ServiceMethod aServiceMethod = new ServiceMethod(aMethod);
                    myServiceMethods[aMethod.Name] = aServiceMethod;
                }
            }
        }


        // Associate the service stub with the input channel which will be then used to send
        // messages to the client(s).
        public void AttachInputChannel(IDuplexInputChannel inputChannel)
        {
            using (EneterTrace.Entering())
            {
                myInputChannel = inputChannel;

                // Find events in the service interface and subscribe to them.
                EventInfo[] anEvents = typeof(TServiceInterface).GetEvents();
                foreach (EventInfo anEventInfo in anEvents)
                {
                    Type anEventArgsType = anEventInfo.EventHandlerType.IsGenericType ?
                        anEventInfo.EventHandlerType.GetGenericArguments()[0] :
                        typeof(EventArgs);

                    // This handler will be subscribed to events from the service.
                    // Note: for each loop create a new local variable so that the context is preserved for the Action<,> event handler.
                    //       if anEventInfo is used then the reference would be changed.
                    EventInfo aTmpEventInfo = anEventInfo;
                    Action<object, EventArgs> anEventHandler = (sender, e) =>
                    {
                        using (EneterTrace.Entering())
                        {
                            string[] aSubscribedClients = null;
                            lock (myServiceEvents)
                            {
                                EventContext anEventContextTmp = myServiceEvents.FirstOrDefault(x => x.EventInfo.Name == aTmpEventInfo.Name);
                                if (anEventContextTmp != null)
                                {
                                    aSubscribedClients = anEventContextTmp.SubscribedClients.ToArray();
                                }
                            }

                            // If some client is subscribed.
                            if (aSubscribedClients != null && aSubscribedClients.Length > 0)
                            {
                                object aSerializedEvent = null;
                                try
                                {
                                    // Serialize the event and send it to subscribed clients.
                                    RpcMessage anEventMessage = new RpcMessage()
                                    {
                                        Id = 0, // dummy - because we do not need to track it.
                                        Flag = RpcFlags.RaiseEvent,
                                        OperationName = aTmpEventInfo.Name,
                                        SerializedData = (anEventArgsType == typeof(EventArgs)) ?
                                            null : // EventArgs is a known type without parameters - we do not need to serialize it.
                                            new object[] { mySerializer.Serialize(anEventArgsType, e) }
                                    };
                                    aSerializedEvent = mySerializer.Serialize<RpcMessage>(anEventMessage);
                                }
                                catch (Exception err)
                                {
                                    EneterTrace.Error(TracedObject + "failed to serialize the event '" + aTmpEventInfo.Name + "'.", err);

                                    // Note: this exception will be thrown to the delegate that raised the event.
                                    throw;
                                }

                                // Iterate via subscribed clients and send them the event.
                                foreach (string aClient in aSubscribedClients)
                                {
                                    try
                                    {
                                        myInputChannel.SendResponseMessage(aClient, aSerializedEvent);
                                    }
                                    catch (Exception err)
                                    {
                                        EneterTrace.Error(TracedObject + "failed to send event to the client.", err);

                                        // Suppose the client is disconnected so unsubscribe it from all events.
                                        UnsubscribeClientFromEvents(aClient);
                                    }
                                }
                            }
                        }
                    };

                    EventContext anEventContext = null;
                    try
                    {
                        anEventContext = new EventContext(myService, anEventInfo, Delegate.CreateDelegate(anEventInfo.EventHandlerType, anEventHandler.Target, anEventHandler.Method));
                        anEventContext.Subscribe();
                    }
                    catch (Exception err)
                    {
                        string anErrorMessage = TracedObject + "failed to attach the output channel because it failed to create EventContext.";
                        EneterTrace.Error(anErrorMessage, err);
                        throw;
                    }

                    lock (myServiceEvents)
                    {
                        if (!myServiceEvents.Add(anEventContext))
                        {
                            string anErrorMessage = TracedObject + "failed to attach the output channel because it failed to create the event '" + anEventInfo.Name + "' because the event already exists.";
                            EneterTrace.Error(anErrorMessage);
                            throw new InvalidOperationException(anErrorMessage);
                        }
                    }
                }
            }
        }

        public void DetachInputChannel()
        {
            using (EneterTrace.Entering())
            {
                // Clean subscription for all clients.
                lock (myServiceEvents)
                {
                    foreach (EventContext anEventContext in myServiceEvents)
                    {
                        try
                        {
                            anEventContext.Unsubscribe();
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + "failed to unsubscribe from the event '" + anEventContext.EventInfo.Name + "'.", err);
                        }
                    }

                    myServiceEvents.Clear();
                }

                myInputChannel = null;
            }
        }

        public void ProcessRemoteRequest(DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                // Deserialize the incoming message.
                RpcMessage aRequestMessage = null;
                try
                {
                    aRequestMessage = mySerializer.Deserialize<RpcMessage>(e.Message);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to deserialize incoming request message.", err);
                    return;
                }

                RpcMessage aResponseMessage = new RpcMessage()
                {
                    Id = aRequestMessage.Id,
                    Flag = RpcFlags.MethodResponse
                };

                // If it is a remote call of a method/function.
                if (aRequestMessage.Flag == RpcFlags.InvokeMethod)
                {
                    EneterTrace.Debug("RPC RECEIVED");

                    // Get the method from the service that shall be invoked.
                    ServiceMethod aServiceMethod;
                    myServiceMethods.TryGetValue(aRequestMessage.OperationName, out aServiceMethod);
                    if (aServiceMethod != null)
                    {
                        if (aRequestMessage.SerializedData != null && aRequestMessage.SerializedData.Length == aServiceMethod.InputParameterTypes.Length)
                        {
                            // Deserialize input parameters.
                            object[] aDeserializedInputParameters = new object[aServiceMethod.InputParameterTypes.Length];
                            try
                            {
                                for (int i = 0; i < aServiceMethod.InputParameterTypes.Length; ++i)
                                {
                                    aDeserializedInputParameters[i] = mySerializer.Deserialize(aServiceMethod.InputParameterTypes[i], aRequestMessage.SerializedData[i]);
                                }
                            }
                            catch (Exception err)
                            {
                                string anErrorMessage = "failed to deserialize input parameters for '" + aRequestMessage.OperationName + "'.";
                                EneterTrace.Error(anErrorMessage, err);

                                aResponseMessage.Error = anErrorMessage + "\n" + err.ToString();
                            }

                            if (string.IsNullOrEmpty(aResponseMessage.Error))
                            {
                                object aResult = null;
                                try
                                {
                                    // Invoke the service method.
                                    aResult = aServiceMethod.Method.Invoke(myService, aDeserializedInputParameters);
                                }
                                catch (Exception err)
                                {
                                    // Note: Use InnerException to skip the wrapping ReflexionException.
                                    Exception ex = (err.InnerException != null) ? err.InnerException : err;

                                    EneterTrace.Error(TracedObject + ErrorHandler.DetectedException, ex);

                                    // The exception will be responded to the client.
                                    aResponseMessage.Error = ex.ToString();
                                }

                                if (string.IsNullOrEmpty(aResponseMessage.Error))
                                {
                                    try
                                    {
                                        // Serialize the result.
                                        object aSerializedReturnValue = (aServiceMethod.Method.ReturnType != typeof(void)) ?
                                            mySerializer.Serialize(aServiceMethod.Method.ReturnType, aResult) :
                                            null;

                                        aResponseMessage.SerializedData = new object[] { aSerializedReturnValue };
                                    }
                                    catch (Exception err)
                                    {
                                        string anErrorMessage = TracedObject + "failed to serialize the result.";
                                        EneterTrace.Error(anErrorMessage, err);

                                        aResponseMessage.Error = anErrorMessage + "\r\n" + err.ToString();
                                    }
                                }
                            }
                        }
                        else
                        {
                            aRequestMessage.Error = TracedObject + "failed to process '" + aRequestMessage.OperationName + "' because it has incorrect number of input parameters.";
                            EneterTrace.Error(aRequestMessage.Error);
                        }
                    }
                    else
                    {
                        aResponseMessage.Error = "Method '" + aRequestMessage.OperationName + "' does not exist in the service.";
                        EneterTrace.Error(TracedObject + "failed to invoke the service method because the method '" + aRequestMessage.OperationName + "' was not found in the service.");
                    }
                }
                // If it is a request to subscribe/unsubcribe an event.
                else if (aRequestMessage.Flag == RpcFlags.SubscribeEvent || aRequestMessage.Flag == RpcFlags.UnsubscribeEvent)
                {
                    EventContext anEventContext = null;
                    lock (myServiceEvents)
                    {
                        anEventContext = myServiceEvents.FirstOrDefault(x => x.EventInfo.Name == aRequestMessage.OperationName);
                        if (anEventContext != null)
                        {
                            if (aRequestMessage.Flag == RpcFlags.SubscribeEvent)
                            {
                                EneterTrace.Debug("SUBSCRIBE REMOTE EVENT RECEIVED");

                                // Note: Events are added to the HashSet.
                                //       Therefore it is ensured each client is subscribed only once.
                                anEventContext.SubscribedClients.Add(e.ResponseReceiverId);
                            }
                            else
                            {
                                EneterTrace.Debug("UNSUBSCRIBE REMOTE EVENT RECEIVED");

                                anEventContext.SubscribedClients.Remove(e.ResponseReceiverId);
                            }
                        }
                    }

                    if (anEventContext == null)
                    {
                        aResponseMessage.Error = TracedObject + "Event '" + aRequestMessage.OperationName + "' does not exist in the service.";
                        EneterTrace.Error(aResponseMessage.Error);
                    }
                }
                else
                {
                    aResponseMessage.Error = TracedObject + "could not recognize the incoming request. If it is RPC, Subscribing or Unsubscribfing.";
                    EneterTrace.Error(aResponseMessage.Error);
                }


                try
                {
                    // Serialize the response message.
                    object aSerializedResponse = mySerializer.Serialize<RpcMessage>(aResponseMessage);
                    myInputChannel.SendResponseMessage(e.ResponseReceiverId, aSerializedResponse);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.SendResponseFailure, err);
                }
            }
        }

        public void UnsubscribeClientFromEvents(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                lock (myServiceEvents)
                {
                    foreach (EventContext anEventContext in myServiceEvents)
                    {
                        anEventContext.SubscribedClients.Remove(responseReceiverId);
                    }
                }
            }
        }

        private ISerializer mySerializer;
        private TServiceInterface myService;
        private HashSet<EventContext> myServiceEvents = new HashSet<EventContext>();
        private Dictionary<string, ServiceMethod> myServiceMethods = new Dictionary<string, ServiceMethod>();
        private IDuplexInputChannel myInputChannel;

        private string TracedObject { get { return GetType().Name + " "; } }
    }
}


#endif