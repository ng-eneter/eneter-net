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

namespace Eneter.Messaging.EndPoints.Rpc
{
    internal class RpcClient<TServiceInterface> : AttachableDuplexOutputChannelBase, IRpcClient<TServiceInterface>
        where TServiceInterface : class
    {
        // Represents the context of an active remote call.
        private class RemoteCallContext
        {
            public RemoteCallContext(string name)
            {
                RpcCompleted = new ManualResetEvent(false);
                MethodName = name;
            }

            public ManualResetEvent RpcCompleted { get; private set; }

            public string MethodName { get; private set; }
            public Exception Error { get; set; }
            public object SerializedReturnValue { get; set; }
        }

        // Provides info about a remote event and maintains subscribers for that event.
        private class RemoteEvent
        {
            public RemoteEvent(Type eventArgsType)
            {
                EventArgsType = eventArgsType;
                Subscribers = new Dictionary<Delegate, Action<object, EventArgs>>();
                Lock = new object();
            }

            public Type EventArgsType { get; private set; }
            public Dictionary<Delegate, Action<object, EventArgs>> Subscribers { get; private set; }
            public object Lock { get; private set; }
        }


        public event EventHandler<DuplexChannelEventArgs> ConnectionOpened;
        public event EventHandler<DuplexChannelEventArgs> ConnectionClosed;


        public RpcClient(ISerializer serializer, TimeSpan rpcTimeout)
        {
            using (EneterTrace.Entering())
            {
                mySerializer = serializer;
                myRpcTimeout = rpcTimeout;

#if !SILVERLIGHT && !COMPACT_FRAMEWORK
                // Dynamically implement and instantiate the given interface as the proxy.
                Proxy = ProxyProvider.CreateInstance<TServiceInterface>(CallMethod, SubscribeEvent, UnsubscribeEvent);
#endif

                // Store remote methods.
                foreach (MethodInfo aMethodInfo in typeof(TServiceInterface).GetMethods())
                {
                    Type aReturnType = aMethodInfo.ReturnType;
                    myRemoteMethodReturnTypes[aMethodInfo.Name] = aReturnType;
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

                myRaiseEventInvoker = new SyncDispatcher();
            }
        }

#if !SILVERLIGHT
        public TServiceInterface Proxy { get; private set; }
#endif

        public override void AttachDuplexOutputChannel(IDuplexOutputChannel duplexOutputChannel)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    duplexOutputChannel.ConnectionOpened += OnConnectionOpened;
                    duplexOutputChannel.ConnectionClosed += OnConnectionClosed;

                    try
                    {
                        base.AttachDuplexOutputChannel(duplexOutputChannel);
                    }
                    catch
                    {
                        EneterTrace.Error(TracedObject + "failed to attach duplex output channel.");

                        try
                        {
                            DetachDuplexOutputChannel();
                        }
                        catch
                        {
                        }

                        throw;
                    }
                }
            }
        }

        public override void DetachDuplexOutputChannel()
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    IDuplexOutputChannel anAttachedDuplexOutputChannel = AttachedDuplexOutputChannel;

                    base.DetachDuplexOutputChannel();

                    if (anAttachedDuplexOutputChannel != null)
                    {
                        anAttachedDuplexOutputChannel.ConnectionOpened -= OnConnectionOpened;
                        anAttachedDuplexOutputChannel.ConnectionClosed -= OnConnectionClosed;
                    }
                }
            }
        }

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


        private void OnConnectionOpened(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                // Recover remote subscriptions at service.
                foreach (KeyValuePair<string, RemoteEvent> aRemoteEvent in myRemoteEvents)
                {
                    lock (aRemoteEvent.Value.Lock)
                    {
                        if (aRemoteEvent.Value.Subscribers.Count > 0)
                        {
                            SubscribeAtService(aRemoteEvent.Key);
                        }
                    }
                }

                // Forward the event.
                Notify(ConnectionOpened, e);
            }
        }

        private void OnConnectionClosed(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                // Forward the event.
                Notify(ConnectionClosed, e);
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
                    aMessage = mySerializer.Deserialize<RpcMessage>(e.Message);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to deserialize incoming message.", err);
                    return;
                }

                // If it is a response for a call.
                if (aMessage.Flag == RpcFlags.MethodResponse)
                {
                    // Try to find if there is a pending request waiting for the response.
                    RemoteCallContext anRpcContext;
                    lock (myPendingRemoteCalls)
                    {
                        myPendingRemoteCalls.TryGetValue(aMessage.Id, out anRpcContext);
                    }

                    if (anRpcContext != null)
                    {
                        if (string.IsNullOrEmpty(aMessage.Error))
                        {
                            if (aMessage.SerializedData != null && aMessage.SerializedData.Length > 0)
                            {
                                anRpcContext.SerializedReturnValue = aMessage.SerializedData[0];
                            }
                            else
                            {
                                anRpcContext.SerializedReturnValue = null;
                            }
                        }
                        else
                        {
                            InvalidOperationException anException = new InvalidOperationException("Detected exception from the service:\n" + aMessage.Error);
                            anRpcContext.Error = anException;
                        }

                        // Release the pending request.
                        anRpcContext.RpcCompleted.Set();
                    }
                }
                else if (aMessage.Flag == RpcFlags.RaiseEvent)
                {
                    if (aMessage.SerializedData != null && aMessage.SerializedData.Length > 0)
                    {
                        // Try to raise an event.
                        // The event is raised in its own thread so that the receiving thread is not blocked.
                        myRaiseEventInvoker.Invoke(() => RaiseEvent(aMessage.OperationName, aMessage.SerializedData[0]));
                    }
                    else
                    {
                        // Note: this happens if the event is of type EventErgs.
                        // The event is raised in its own thread so that the receiving thread is not blocked.
                        myRaiseEventInvoker.Invoke(() => RaiseEvent(aMessage.OperationName, null));
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
                // Serialize method parameters.
                object[] aSerialzedMethodParameters = new object[parameters.Length];
                try
                {
                    for (int i = 0; i < parameters.Length; ++i)
                    {
                        aSerialzedMethodParameters[i] = mySerializer.Serialize(parameters[i].GetType(), parameters[i]);
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
                aRequestMessage.Flag = RpcFlags.InvokeMethod;
                aRequestMessage.OperationName = methodName;
                aRequestMessage.SerializedData = aSerialzedMethodParameters;

                object aSerializedReturnValue = CallService(aRequestMessage);

                // Get the type of the return value.
                Type aReturnType;
                myRemoteMethodReturnTypes.TryGetValue(aRequestMessage.OperationName, out aReturnType);
                if (aReturnType == null)
                {
                    string anErrorMessage = TracedObject + "failed to deserialize the received return value. The method '" + aRequestMessage.OperationName + "' was not found.";
                    EneterTrace.Error(anErrorMessage);
                    throw new InvalidOperationException(anErrorMessage);
                }

                // Deserialize the return value.
                object aDeserializedReturnValue = null;
                try
                {
                    aDeserializedReturnValue = (aReturnType != typeof(void)) ?
                        mySerializer.Deserialize(aReturnType, aSerializedReturnValue) :
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

                lock (aRemoteEvent.Lock)
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

                lock (aServiceEvent.Lock)
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
                        aRequestMessage.Flag = RpcFlags.UnsubscribeEvent;
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
                    aSubscribeMessage.Flag = RpcFlags.SubscribeEvent;
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
                    string anError = TracedObject + ErrorHandler.ChannelNotAttached;
                    EneterTrace.Error(anError);
                    throw new InvalidOperationException(anError);
                }

                try
                {
                    RemoteCallContext anRpcSyncContext = new RemoteCallContext(rpcRequest.OperationName);
                    lock (myPendingRemoteCalls)
                    {
                        myPendingRemoteCalls.Add(rpcRequest.Id, anRpcSyncContext);
                    }

                    // Send the request.
                    object aSerializedMessage = mySerializer.Serialize<RpcMessage>(rpcRequest);
                    AttachedDuplexOutputChannel.SendMessage(aSerializedMessage);

                    // Wait for the response.
                    if (!anRpcSyncContext.RpcCompleted.WaitOne((int)myRpcTimeout.TotalMilliseconds))
                    {
                        throw new TimeoutException("Remote call to '" + rpcRequest.OperationName + "' has not returned within the specified timeout " + myRpcTimeout + ".");
                    }

                    if (anRpcSyncContext.Error != null)
                    {
                        throw anRpcSyncContext.Error;
                    }

                    return anRpcSyncContext.SerializedReturnValue;
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.SendMessageFailure, err);
                    throw;
                }
                finally
                {
                    lock (myPendingRemoteCalls)
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
                        (EventArgs) mySerializer.Deserialize(aRemoteEvent.EventArgsType, serializedEventArgs);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to deserialize the event '" + name + "'.", err);
                    return;
                }

                // Notify all subscribers.
                lock (aRemoteEvent.Lock)
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
        private IThreadDispatcher myRaiseEventInvoker;
        private object myConnectionManipulatorLock = new object();
        private TimeSpan myRpcTimeout;

        private Dictionary<string, Type> myRemoteMethodReturnTypes = new Dictionary<string, Type>();
        private Dictionary<string, RemoteEvent> myRemoteEvents = new Dictionary<string, RemoteEvent>();
        
        protected override string TracedObject { get { return GetType().Name + " "; } }
    }
}
