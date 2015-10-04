/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    internal class DuplexTypedMessageSender<_ResponseType, _RequestType> : AttachableDuplexOutputChannelBase, IDuplexTypedMessageSender<_ResponseType, _RequestType>
    {
        public event EventHandler<DuplexChannelEventArgs> ConnectionOpened;
        public event EventHandler<DuplexChannelEventArgs> ConnectionClosed;
        public event EventHandler<TypedResponseReceivedEventArgs<_ResponseType>> ResponseReceived;


        public DuplexTypedMessageSender(ISerializer serializer)
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
            }
        }

        public void SendRequestMessage(_RequestType message)
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
                    object aRequestMessage = mySerializer.ForResponseReceiver(AttachedDuplexOutputChannel.ResponseReceiverId).Serialize<_RequestType>(message);
                    AttachedDuplexOutputChannel.SendMessage(aRequestMessage);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.FailedToSendMessage, err);
                    throw;
                }
            }
        }

        protected override void OnResponseMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (ResponseReceived == null)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.NobodySubscribedForMessage);
                    return;
                }

                TypedResponseReceivedEventArgs<_ResponseType> aResponseReceivedEventArgs = null;

                try
                {
                    _ResponseType aResponseMessage = mySerializer.ForResponseReceiver(AttachedDuplexOutputChannel.ResponseReceiverId).Deserialize<_ResponseType>(e.Message);
                    aResponseReceivedEventArgs = new TypedResponseReceivedEventArgs<_ResponseType>(aResponseMessage);
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + "failed to deserialize the response message.", err);
                    aResponseReceivedEventArgs = new TypedResponseReceivedEventArgs<_ResponseType>(err);
                }

                try
                {
                    ResponseReceived(this, aResponseReceivedEventArgs);
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                }
            }
        }

        protected override void OnConnectionOpened(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                Notify(ConnectionOpened, e);
            }
        }

        protected override void OnConnectionClosed(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                Notify(ConnectionClosed, e);
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

        protected override string TracedObject
        {
            get
            {
                return GetType().Name + "<" + typeof(_ResponseType).Name + ", " + typeof(_RequestType).Name + "> ";
            }
        }
    }
}
