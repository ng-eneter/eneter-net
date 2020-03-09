/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.Threading.Dispatching;
using Eneter.Messaging.Threading;

namespace Eneter.Messaging.EndPoints.Rpc
{
    internal class RpcClient<TServiceInterface> : AttachableDuplexOutputChannelBase, IRpcClient<TServiceInterface>
        where TServiceInterface : class
    {
        // Represents the context of an active remote call.
        private class RemoteCallContext
        {
            public RemoteCallContext()
            {
                RpcCompleted = new ManualResetEvent(false);
            }

            public ManualResetEvent RpcCompleted { get; private set; }

            public Exception Error { get; set; }
            public object SerializedReturnValue { get; set; }
        }

        private class RemoteMethod
        {
            public RemoteMethod(Type returnType, Type[] argTypes)
            {
                ReturnType = returnType;
                ArgTypes = argTypes;
            }

            public Type[] ArgTypes { get; private set; }
            public Type ReturnType { get; private set; }
        }

        // Provides info about a remote event and maintains subscribers for that event.
        private class RemoteEvent
        {
            public RemoteEvent(Type eventArgsType)
            {
                EventArgsType = eventArgsType;
                Subscribers = new Dictionary<Delegate, Action<object, EventArgs>>();
                SubscribeUnsubscribeLock = new object();
            }

            public Type EventArgsType { get; private set; }
            public Dictionary<Delegate, Action<object, EventArgs>> Subscribers { get; private set; }
            public object SubscribeUnsubscribeLock { get; private set; }
        }


        public event EventHandler<DuplexChannelEventArgs> ConnectionOpened;
        public event EventHandler<DuplexChannelEventArgs> ConnectionClosed;


        public RpcClient(ISerializer serializer, TimeSpan rpcTimeout, IThreadDispatcher threadDispatcher)
        {
            using (EneterTrace.Entering())
            {
                if (serializer == null)
                {
                    string anError = "Input parameter serializer is null.";
                    EneterTrace.Error(anError);
                    throw new ArgumentNullException(anError);
                }

                mySerializer = serializer;
                myRpcTimeout = rpcTimeout;

#if !NETSTANDARD
                ServiceInterfaceChecker.CheckForClient<TServiceInterface>();

                // Dynamically implement and instantiate the given interface as the proxy.
                Proxy = ProxyProvider.CreateInstance<TServiceInterface>(CallMethod, SubscribeEvent, UnsubscribeEvent);
#endif

                // Store remote methods.
                foreach (MethodInfo aMethodInfo in typeof(TServiceInterface).GetMethods())
                {
                    Type aReturnType = aMethodInfo.ReturnType;
                    Type[] anArgTypes = aMethodInfo.GetParameters().Select(x => x.ParameterType).ToArray();
                    RemoteMethod aRemoteMethod = new RemoteMethod(aReturnType, anArgTypes);
                    myRemoteMethods[aMethodInfo.Name] = aRemoteMethod;
                }

                // Store remote events.
                foreach (EventInfo anEventInfo in typeof(TServiceInterface).GetEvents())
                {
                    if (anEventInfo.EventHandlerType.IsGenericType)
                    {
                        RemoteEvent aRemoteEvent = new RemoteEvent(anEventInfo.EventHandlerType.GetGenericArguments()[0]);
                        myRemoteEvents[anEventInfo.Name] = aRemoteEvent;
                    }
                    else
                    {
                        RemoteEvent aRemoteEvent = new RemoteEvent(typeof(EventArgs));
                        myRemoteEvents[anEventInfo.Name] = aRemoteEvent;
                    }
                }

                myThreadDispatcher = threadDispatcher;
            }
        }

#if !NETSTANDARD
        public TServiceInterface Proxy { get; private set; }
#endif
        public void SubscribeRemoteEvent<TEventArgs>(string eventName, EventHandler<TEventArgs> eventHandler)
            where TEventArgs : EventArgs
        {
            using (EneterTrace.Entering())
            {
                Action<object, EventArgs> aHandler = (x, y) => eventHandler(x, (TEventArgs) y);
                SubscribeEvent(eventName, eventHandler, aHandler);
            }
        }

        public void UnsubscribeRemoteEvent(string eventName, Delegate eventHandler)
        {
            using (EneterTrace.Entering())
            {
                UnsubscribeEvent(eventName, eventHandler);
            }
        }

        public object CallRemoteMethod(string methodName, params object[] args)
        {
            using (EneterTrace.Entering())
            {
                object aResult = CallMethod(methodName, args);
                return aResult;
            }
        }

        protected override void OnConnectionOpened(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                // Note: Following subscribing cannot block the thread processing events from duplex output channel
                //       because a deadlock can occur. Because if the lock would stop this thread other messages could not be processed.
                EneterThreadPool.QueueUserWorkItem(() =>
                    {
                        // Recover remote subscriptions at service.
                        foreach (KeyValuePair<string, RemoteEvent> aRemoteEvent in myRemoteEvents)
                        {
                            // Note: In Java, the following 'lock' section is located in RemoteEvent class.
                            //       It is not possible to locate it there in C# because inner class cannot reach methods of outer class.
                            using (ThreadLock.Lock(aRemoteEvent.Value.SubscribeUnsubscribeLock))
                            {
                                if (aRemoteEvent.Value.Subscribers.Count > 0)
                                {
                                    SubscribeAtService(aRemoteEvent.Key);
                                }
                            }
                        }

                        // Forward the event.
                        myThreadDispatcher.Invoke(() => Notify(ConnectionOpened, e));
                    });
            }
        }

        protected override void OnConnectionClosed(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                // Release all pending RPC calls.
                RemoteCallContext[] aPendingCalls;
                using (ThreadLock.Lock(myPendingRemoteCalls))
                {
                    aPendingCalls = myPendingRemoteCalls.Values.ToArray();
                }
                if (aPendingCalls != null)
                {
                    RpcException anException = new RpcException("Connection was broken or closed.", "", "");
                    foreach (RemoteCallContext aRemoteCallContext in aPendingCalls)
                    {
                        aRemoteCallContext.Error = anException;
                        aRemoteCallContext.RpcCompleted.Set();
                    }
                }

                // Forward the event.
                myThreadDispatcher.Invoke(() => Notify(ConnectionClosed, e));
            }
        }


        // This method is called when a message from the service is received.
        // This can be either the response for a request or it can be an event raised in the service.
        protected override void OnResponseMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                RpcMessage aMessage = null;
                try
                {
                    aMessage = mySerializer.ForResponseReceiver(e.ResponseReceiverId).Deserialize<RpcMessage>(e.Message);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to deserialize incoming message.", err);
                    return;
                }

                // If it is a response for a call.
                if (aMessage.Request == ERpcRequest.Response)
                {
                    EneterTrace.Debug("RETURN FROM RPC RECEIVED");

                    // Try to find if there is a pending request waiting for the response.
                    RemoteCallContext anRpcContext;
                    using (ThreadLock.Lock(myPendingRemoteCalls))
                    {
                        myPendingRemoteCalls.TryGetValue(aMessage.Id, out anRpcContext);
                    }

                    if (anRpcContext != null)
                    {
                        if (string.IsNullOrEmpty(aMessage.ErrorType))
                        {
                            anRpcContext.SerializedReturnValue = aMessage.SerializedReturn;
                        }
                        else
                        {
                            RpcException anException = new RpcException(aMessage.ErrorMessage, aMessage.ErrorType, aMessage.ErrorDetails);
                            anRpcContext.Error = anException;
                        }

                        // Release the pending request.
                        anRpcContext.RpcCompleted.Set();
                    }
                }
                else if (aMessage.Request == ERpcRequest.RaiseEvent)
                {
                    EneterTrace.Debug("EVENT FROM SERVICE RECEIVED");

                    if (aMessage.SerializedParams != null && aMessage.SerializedParams.Length > 0)
                    {
                        // Try to raise an event.
                        // The event is raised in its own thread so that the receiving thread is not blocked.
                        // Note: raising an event cannot block handling of response messages because it can block
                        //       processing of an RPC response for which the RPC caller thread is waiting.
                        //       And if this waiting caller thread is a thread where events are routed and if the routing
                        //       of these events is 'blocking' then a deadlock can occur.
                        //       Therefore ThreadPool is used.
                        EneterThreadPool.QueueUserWorkItem(() => myThreadDispatcher.Invoke(() => RaiseEvent(aMessage.OperationName, aMessage.SerializedParams[0])));
                    }
                    else
                    {
                         // Note: this happens if the event is of type EventErgs.
                        // The event is raised in its own thread so that the receiving thread is not blocked.
                        EneterThreadPool.QueueUserWorkItem(() => myThreadDispatcher.Invoke(() => RaiseEvent(aMessage.OperationName, null)));
                    }
                }
                else
                {
                    EneterTrace.Warning(TracedObject + "detected a message with unknown flag number.");
                }
            }
        }


        private object CallMethod(string methodName, object[] parameters)
        {
            using (EneterTrace.Entering())
            {
                RemoteMethod aRemoteMethod;
                myRemoteMethods.TryGetValue(methodName, out aRemoteMethod);
                if (aRemoteMethod == null)
                {
                    string anErrorMessage = TracedObject + "failed to call remote method '" + methodName + "' because the method is not declared in the service interface on the client side.";
                    EneterTrace.Error(anErrorMessage);
                    throw new InvalidOperationException(anErrorMessage);
                }

                string aResponseReceiverId = AttachedDuplexOutputChannel.ResponseReceiverId;

                // Serialize method parameters.
                object[] aSerialzedMethodParameters = new object[parameters.Length];
                try
                {
                    for (int i = 0; i < parameters.Length; ++i)
                    {
                        // The parameter can be null, therefore we need to get the parameter type from the method declaration
                        // and not from the parameter instance.
                        aSerialzedMethodParameters[i] = mySerializer.ForResponseReceiver(aResponseReceiverId).Serialize(aRemoteMethod.ArgTypes[i], parameters[i]);
                    }
                }
                catch (Exception err)
                {
                    string anErrorMessage = TracedObject + "failed to serialize method parameters.";
                    EneterTrace.Error(anErrorMessage, err);
                    throw;
                }

                // Create message asking the service to execute the method.
                RpcMessage aRequestMessage = new RpcMessage();
                aRequestMessage.Id = Interlocked.Increment(ref myCounter);
                aRequestMessage.Request = ERpcRequest.InvokeMethod;
                aRequestMessage.OperationName = methodName;
                aRequestMessage.SerializedParams = aSerialzedMethodParameters;

                object aSerializedReturnValue = CallService(aRequestMessage);

                // Deserialize the return value.
                object aDeserializedReturnValue = null;
                try
                {
                    aDeserializedReturnValue = (aRemoteMethod.ReturnType != typeof(void)) ?
                        mySerializer.ForResponseReceiver(aResponseReceiverId).Deserialize(aRemoteMethod.ReturnType, aSerializedReturnValue) :
                        null;
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to deserialize the return value.", err);
                    throw;
                }

                return aDeserializedReturnValue;
            }
        }

        private void SubscribeEvent(string eventName, Delegate handler, Action<object, EventArgs> handlerWrapper)
        {
            using (EneterTrace.Entering())
            {
                // Find the event and check if it is already subscribed at the service.
                RemoteEvent aRemoteEvent;
                myRemoteEvents.TryGetValue(eventName, out aRemoteEvent);
                if (aRemoteEvent == null)
                {
                    string anErrorMessage = TracedObject + "failed to subscribe. The event '" + eventName + "' does not exist.";
                    EneterTrace.Error(anErrorMessage);
                    throw new InvalidOperationException(anErrorMessage);
                }


                // Note: In Java, the following 'lock' section is located in RemoteEvent class.
                //       It is not possible to locate it there in C# because inner class cannot reach methods of outer class.
                using (ThreadLock.Lock(aRemoteEvent.SubscribeUnsubscribeLock))
                {
                    // Store the subscriber.
                    aRemoteEvent.Subscribers[handler] = handlerWrapper;

                    // If it is the first subscriber then try to subscribe at service.
                    if (aRemoteEvent.Subscribers.Count == 1)
                    {
                        if (IsDuplexOutputChannelAttached && AttachedDuplexOutputChannel.IsConnected)
                        {
                            try
                            {
                                SubscribeAtService(eventName);
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + "failed to subscribe at service. Eventhandler is subscribed just locally in the proxy.", err);
                            }
                        }
                    }
                }
            }
        }

        

        private void UnsubscribeEvent(string eventName, Delegate handler)
        {
            using (EneterTrace.Entering())
            {
                // Find the event and check if it is already subscribed at the service.
                RemoteEvent aServiceEvent;
                myRemoteEvents.TryGetValue(eventName, out aServiceEvent);
                if (aServiceEvent == null)
                {
                    EneterTrace.Warning(TracedObject + "failed to unsubscribe. The event '" + eventName + "' does not exist."); 
                    return;
                }

                // Note: In Java, the following 'lock' section is located in RemoteEvent class.
                //       It is not possible to locate it there in C# because inner class cannot reach methods of outer class.
                using (ThreadLock.Lock(aServiceEvent.SubscribeUnsubscribeLock))
                {
                    // Remove the subscriber from the list.
                    aServiceEvent.Subscribers.Remove(handler);

                    // If it was the last subscriber then unsubscribe at the service.
                    // Note: unsubscribing from the service prevents sending of notifications across the network
                    //       if nobody is subscribed.
                    if (!aServiceEvent.Subscribers.Any())
                    {
                        // Create message asking the service to unsubscribe from the event.
                        RpcMessage aRequestMessage = new RpcMessage();
                        aRequestMessage.Id = Interlocked.Increment(ref myCounter);
                        aRequestMessage.Request = ERpcRequest.UnsubscribeEvent;
                        aRequestMessage.OperationName = eventName;

                        try
                        {
                            CallService(aRequestMessage);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + "failed to unsubscribe from the service.", err);
                        }
                    }
                }
            }
        }

        private void SubscribeAtService(string eventName)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    // Create message asking the service to subscribe for the event.
                    RpcMessage aSubscribeMessage = new RpcMessage();
                    aSubscribeMessage.Id = Interlocked.Increment(ref myCounter);
                    aSubscribeMessage.Request = ERpcRequest.SubscribeEvent;
                    aSubscribeMessage.OperationName = eventName;

                    // Send the subscribing request to the service.
                    CallService(aSubscribeMessage);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to subscribe '" + eventName + "' event at the service.", err);
                    throw;
                }
            }
        }

        private object CallService(RpcMessage rpcRequest)
        {
            using (EneterTrace.Entering())
            {
                if (AttachedDuplexOutputChannel == null)
                {
                    string anError = TracedObject + ErrorHandler.FailedToSendMessageBecauseNotAttached;
                    EneterTrace.Error(anError);
                    throw new InvalidOperationException(anError);
                }

                try
                {
                    RemoteCallContext anRpcSyncContext = new RemoteCallContext();
                    using (ThreadLock.Lock(myPendingRemoteCalls))
                    {
                        myPendingRemoteCalls.Add(rpcRequest.Id, anRpcSyncContext);
                    }

                    // Send the request.
                    object aSerializedMessage = mySerializer.ForResponseReceiver(AttachedDuplexOutputChannel.ResponseReceiverId).Serialize<RpcMessage>(rpcRequest);
                    AttachedDuplexOutputChannel.SendMessage(aSerializedMessage);

                    // Wait for the response.
                    if (!anRpcSyncContext.RpcCompleted.WaitOne((int)myRpcTimeout.TotalMilliseconds))
                    {
                        throw new TimeoutException("Remote call has not returned within the specified timeout " + myRpcTimeout + ".");
                    }

                    if (anRpcSyncContext.Error != null)
                    {
                        throw anRpcSyncContext.Error;
                    }

                    return anRpcSyncContext.SerializedReturnValue;
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "." + rpcRequest.OperationName + "(..) " + ErrorHandler.FailedToSendMessage, err);
                    throw;
                }
                finally
                {
                    using (ThreadLock.Lock(myPendingRemoteCalls))
                    {
                        myPendingRemoteCalls.Remove(rpcRequest.Id);
                    }
                }
            }
        }

        private void RaiseEvent(string name, object serializedEventArgs)
        {
            using (EneterTrace.Entering())
            {
                RemoteEvent aRemoteEvent;
                myRemoteEvents.TryGetValue(name, out aRemoteEvent);
                if (aRemoteEvent == null)
                {
                    EneterTrace.Error(TracedObject + "failed to raise the event. The event '" + name + "' was not found.");
                    return;
                }

                // Get the type of the EventArgs for the incoming event and deserialize it.
                EventArgs anEventArgs = null;
                try
                {
                    anEventArgs = (aRemoteEvent.EventArgsType == typeof(EventArgs)) ?
                        new EventArgs() :
                        (EventArgs) mySerializer.ForResponseReceiver(AttachedDuplexOutputChannel.ResponseReceiverId).Deserialize(aRemoteEvent.EventArgsType, serializedEventArgs);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to deserialize the event '" + name + "'.", err);
                    return;
                }

                // Note: In Java, the following 'lock' section is located in RemoteEvent class.
                //       It is not possible to locate it there in C# because inner class cannot reach methods of outer class.
                // Notify all subscribers.
                using (ThreadLock.Lock(aRemoteEvent.SubscribeUnsubscribeLock))
                {
                    foreach (KeyValuePair<Delegate, Action<object, EventArgs>> aSubscriber in aRemoteEvent.Subscribers)
                    {
                        try
                        {
                            aSubscriber.Value(this, anEventArgs);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                        }
                    }
                }
            }
        }

        private void Notify(EventHandler<DuplexChannelEventArgs> handler, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (handler != null)
                {
                    try
                    {
                        handler(this, e);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }

        private ISerializer mySerializer;
        private int myCounter;
        private Dictionary<int, RemoteCallContext> myPendingRemoteCalls = new Dictionary<int, RemoteCallContext>();
        private IThreadDispatcher myThreadDispatcher;
        private TimeSpan myRpcTimeout;

        private Dictionary<string, RemoteMethod> myRemoteMethods = new Dictionary<string, RemoteMethod>();
        private Dictionary<string, RemoteEvent> myRemoteEvents = new Dictionary<string, RemoteEvent>();
        
        protected override string TracedObject { get { return GetType().Name + "<" + typeof(TServiceInterface).Name + "> "; } }
    }
}
