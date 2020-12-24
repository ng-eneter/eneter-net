

using System;
using System.Collections.Generic;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    internal class MultiTypedMessageReceiver : IMultiTypedMessageReceiver
    {
        private class TMessageHandler
        {
            public TMessageHandler(Type type, Action<string, string, object, Exception> eventInvoker)
            {
                Type = type;
                Invoke = eventInvoker;
            }

            public Type Type { get; private set; }
            public Action<string, string, object, Exception> Invoke { get; private set; }
        }

        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;

        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;


        public MultiTypedMessageReceiver(ISerializer serializer)
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

                IDuplexTypedMessagesFactory aFactory = new DuplexTypedMessagesFactory(serializer);

                myReceiver = aFactory.CreateDuplexTypedMessageReceiver<MultiTypedMessage, MultiTypedMessage>();
                myReceiver.ResponseReceiverConnected += OnResponseReceiverConnected;
                myReceiver.ResponseReceiverDisconnected += OnResponseReceiverDisconnected;
                myReceiver.MessageReceived += OnRequestMessageReceived;
            }
        }

        public void AttachDuplexInputChannel(IDuplexInputChannel duplexInputChannel)
        {
            using (EneterTrace.Entering())
            {
                myReceiver.AttachDuplexInputChannel(duplexInputChannel);
            }
        }

        public void DetachDuplexInputChannel()
        {
            using (EneterTrace.Entering())
            {
                myReceiver.DetachDuplexInputChannel();
            }
        }

        public bool IsDuplexInputChannelAttached
        {
            get { return myReceiver.IsDuplexInputChannelAttached; }
        }

        public IDuplexInputChannel AttachedDuplexInputChannel
        {
            get { return myReceiver.AttachedDuplexInputChannel; }
        }

        public void RegisterRequestMessageReceiver<T>(EventHandler<TypedRequestReceivedEventArgs<T>> handler)
        {
            using (EneterTrace.Entering())
            {
                if (handler == null)
                {
                    string anError = TracedObject + "failed to register handler for message " + typeof(T).Name + " because the input parameter handler is null.";
                    EneterTrace.Error(anError);
                    throw new ArgumentNullException(anError);
                }

                using (ThreadLock.Lock(myMessageHandlers))
                {
                    TMessageHandler aMessageHandler;
                    myMessageHandlers.TryGetValue(typeof(T).Name, out aMessageHandler);

                    if (aMessageHandler != null)
                    {
                        string anError = TracedObject + "failed to register handler for message " + typeof(T).Name + " because the handler for such class name is already registered.";
                        EneterTrace.Error(anError);
                        throw new InvalidOperationException(anError);
                    }

                    // Note: the invoking method must be cached for particular types because
                    //       during deserialization the generic argument is not available and so it would not be possible
                    //       to instantiate TypedRequestReceivedEventArgs<T>.
                    Action<string, string, object, Exception> anEventInvoker = (responseReceiverId, senderAddress, message, receivingError) =>
                        {
                            TypedRequestReceivedEventArgs<T> anEvent;
                            if (receivingError == null)
                            {
                                anEvent = new TypedRequestReceivedEventArgs<T>(responseReceiverId, senderAddress, (T)message);
                            }
                            else
                            {
                                anEvent = new TypedRequestReceivedEventArgs<T>(responseReceiverId, senderAddress, receivingError);
                            }
                            handler(this, anEvent);
                        };
                    myMessageHandlers[typeof(T).Name] = new TMessageHandler(typeof(T), anEventInvoker);
                }
            }
        }

        public void UnregisterRequestMessageReceiver<T>()
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myMessageHandlers))
                {
                    myMessageHandlers.Remove(typeof(T).Name);
                }
            }
        }

        public IEnumerable<Type> RegisteredRequestMessageTypes
        {
            get
            {
                using (ThreadLock.Lock(myMessageHandlers))
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

        public void SendResponseMessage<TResponseMessage>(string responseReceiverId, TResponseMessage responseMessage)
        {
            using (EneterTrace.Entering())
            {
                MultiTypedMessage aMessage = new MultiTypedMessage();
                aMessage.TypeName = typeof(TResponseMessage).Name;
                aMessage.MessageData = mySerializer.ForResponseReceiver(responseReceiverId).Serialize<TResponseMessage>(responseMessage);

                myReceiver.SendResponseMessage(responseReceiverId, aMessage);
            }
        }


        private void OnResponseReceiverConnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (ResponseReceiverConnected != null)
                {
                    ResponseReceiverConnected(this, e);
                }
            }
        }

        private void OnResponseReceiverDisconnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (ResponseReceiverDisconnected != null)
                {
                    ResponseReceiverDisconnected(this, e);
                }
            }
        }

        private void OnRequestMessageReceived(object sender, TypedRequestReceivedEventArgs<MultiTypedMessage> e)
        {
            using (EneterTrace.Entering())
            {
                if (e.ReceivingError != null)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.FailedToReceiveMessage, e.ReceivingError);
                }
                else
                {
                    TMessageHandler aMessageHandler;

                    using (ThreadLock.Lock(myMessageHandlers))
                    {
                        myMessageHandlers.TryGetValue(e.RequestMessage.TypeName, out aMessageHandler);
                    }

                    if (aMessageHandler != null)
                    {
                        object aMessageData;
                        try
                        {
                            aMessageData = mySerializer.ForResponseReceiver(e.ResponseReceiverId).Deserialize(aMessageHandler.Type, e.RequestMessage.MessageData);

                            try
                            {
                                aMessageHandler.Invoke(e.ResponseReceiverId, e.SenderAddress, aMessageData, null);
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                            }
                        }
                        catch (Exception err)
                        {
                            try
                            {
                                aMessageHandler.Invoke(e.ResponseReceiverId, e.SenderAddress, null, err);
                            }
                            catch (Exception err2)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err2);
                            }
                        }
                    }
                    else
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.NobodySubscribedForMessage + " Message type = " + e.RequestMessage.TypeName);
                    }
                }
            }
        }

        private ISerializer mySerializer;
        private IDuplexTypedMessageReceiver<MultiTypedMessage, MultiTypedMessage> myReceiver;

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
