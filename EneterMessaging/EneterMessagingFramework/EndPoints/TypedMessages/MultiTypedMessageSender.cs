/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/

using System;
using System.Collections.Generic;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    internal class MultiTypedMessageSender : IMultiTypedMessageSender
    {
        private class TMessageHandler
        {
            public TMessageHandler(Type type, Action<object, Exception> eventInvoker)
            {
                Type = type;
                Invoke = eventInvoker;
            }

            public Type Type { get; private set; }
            public Action<object, Exception> Invoke { get; private set; }
        }

        public event EventHandler<DuplexChannelEventArgs> ConnectionOpened;

        public event EventHandler<DuplexChannelEventArgs> ConnectionClosed;

        public event EventHandler<TypedResponseReceivedEventArgs<object>> FailedToReceiveResponseMessage;

        public MultiTypedMessageSender(ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                mySerializer = serializer;

                IDuplexTypedMessagesFactory aFactory = new DuplexTypedMessagesFactory(serializer);
                mySender = aFactory.CreateDuplexTypedMessageSender<MultiTypedMessage, MultiTypedMessage>();
                mySender.ConnectionOpened += OnConnectionOpened;
                mySender.ConnectionClosed += OnConnectionClosed;
                mySender.ResponseReceived += OnResponseReceived;
            }
        }

        public void AttachDuplexOutputChannel(IDuplexOutputChannel duplexOutputChannel)
        {
            using (EneterTrace.Entering())
            {
                mySender.AttachDuplexOutputChannel(duplexOutputChannel);
            }
        }

        public void DetachDuplexOutputChannel()
        {
            using (EneterTrace.Entering())
            {
                mySender.DetachDuplexOutputChannel();
            }
        }

        public bool IsDuplexOutputChannelAttached
        {
            get { return mySender.IsDuplexOutputChannelAttached; }
        }

        public IDuplexOutputChannel AttachedDuplexOutputChannel
        {
            get { return mySender.AttachedDuplexOutputChannel; }
        }

        public void RegisterResponseMessageReceiver<T>(EventHandler<TypedResponseReceivedEventArgs<T>> handler)
        {
            using (EneterTrace.Entering())
            {
                if (handler == null)
                {
                    string anError = TracedObject + "failed to register handler for response message " + typeof(T).Name + " because the input parameter handler is null.";
                    EneterTrace.Error(anError);
                    throw new ArgumentNullException(anError);
                }

                lock (myMessageHandlers)
                {
                    TMessageHandler aMessageHandler;
                    myMessageHandlers.TryGetValue(typeof(T).Name, out aMessageHandler);
                    if (aMessageHandler != null)
                    {
                        string anError = TracedObject + "failed to register handler for response message " + typeof(T).Name + " because the handler for such class name is already registered.";
                        EneterTrace.Error(anError);
                        throw new InvalidOperationException(anError);
                    }

                    // Note: the invoking method must be cached for particular types because
                    //       during deserialization the generic argument is not available and so it would not be possible
                    //       to instantiate TypedRequestReceivedEventArgs<T>.
                    Action<object, Exception> anEventInvoker = (message, receivingError) =>
                        {
                            TypedResponseReceivedEventArgs<T> anEvent;
                            if (receivingError == null)
                            {
                                anEvent = new TypedResponseReceivedEventArgs<T>((T)message);
                            }
                            else
                            {
                                anEvent = new TypedResponseReceivedEventArgs<T>(receivingError);
                            }
                            handler(this, anEvent);
                        };
                    myMessageHandlers[typeof(T).Name] = new TMessageHandler(typeof(T), anEventInvoker);

                }
            }
        }

        public void UnregisterResponseMessageReceiver<T>()
        {
            using (EneterTrace.Entering())
            {
                lock (myMessageHandlers)
                {
                    myMessageHandlers.Remove(typeof(T).Name);
                }
            }
        }

        public IEnumerable<Type> RegisteredResponseMessageTypes
        {
            get
            {
                lock (myMessageHandlers)
                {
                    List<Type> aRegisteredMessageTypes = new List<Type>();
                    foreach (TMessageHandler aHandler in myMessageHandlers.Values)
                    {
                        aRegisteredMessageTypes.Add(aHandler.Type);
                    }
                    return aRegisteredMessageTypes;
                }
            }
        }

        public void SendRequestMessage<TRequestMessage>(TRequestMessage message)
        {
            using (EneterTrace.Entering())
            {
                MultiTypedMessage aMessage = new MultiTypedMessage();
                aMessage.TypeName = typeof(TRequestMessage).Name;
                aMessage.MessageData = mySerializer.Serialize<TRequestMessage>(message);

                mySender.SendRequestMessage(aMessage);
            }
        }


        private void OnConnectionOpened(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (ConnectionOpened != null)
                {
                    ConnectionOpened(this, e);
                }
            }
        }

        private void OnConnectionClosed(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (ConnectionClosed != null)
                {
                    ConnectionClosed(this, e);
                }
            }
        }

        private void OnResponseReceived(object sender, TypedResponseReceivedEventArgs<MultiTypedMessage> e)
        {
            using (EneterTrace.Entering())
            {
                if (e.ReceivingError != null)
                {
                    if (FailedToReceiveResponseMessage != null)
                    {
                        TypedResponseReceivedEventArgs<object> anEvent = new TypedResponseReceivedEventArgs<object>(e.ReceivingError);
                        FailedToReceiveResponseMessage(this, anEvent);
                    }
                }
                else
                {
                    TMessageHandler aMessageHandler;

                    lock (myMessageHandlers)
                    {
                        myMessageHandlers.TryGetValue(e.ResponseMessage.TypeName, out aMessageHandler);
                    }

                    if (aMessageHandler != null)
                    {
                        object aMessageData;
                        try
                        {
                            aMessageData = mySerializer.Deserialize(aMessageHandler.Type, e.ResponseMessage.MessageData);

                            try
                            {
                                aMessageHandler.Invoke(aMessageData, null);
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                            }
                        }
                        catch (Exception err)
                        {
                            aMessageHandler.Invoke(null, err);
                        }
                        
                    }
                    else
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.NobodySubscribedForMessage + " Message type = " + e.ResponseMessage.TypeName);
                    }
                }
            }
        }


        private ISerializer mySerializer;
        private IDuplexTypedMessageSender<MultiTypedMessage, MultiTypedMessage> mySender;

        private Dictionary<string, TMessageHandler> myMessageHandlers = new Dictionary<string, TMessageHandler>();

        private string TracedObject
        {
            get
            {
                return GetType().Name + " ";
            }
        }
    }
}
