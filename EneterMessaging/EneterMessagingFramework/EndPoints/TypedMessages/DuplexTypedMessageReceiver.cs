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
    internal class DuplexTypedMessageReceiver<_ResponseType, _RequestType> : AttachableDuplexInputChannelBase, IDuplexTypedMessageReceiver<_ResponseType, _RequestType>
    {
        public event EventHandler<TypedRequestReceivedEventArgs<_RequestType>> MessageReceived;

        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;

        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;


        public DuplexTypedMessageReceiver(ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                mySerializer = serializer;
            }
        }

        public void SendResponseMessage(string responseReceiverId, _ResponseType responseMessage)
        {
            using (EneterTrace.Entering())
            {
                if (AttachedDuplexInputChannel == null)
                {
                    string anError = TracedObject + "failed to send the response message because it is not attached to any duplex input channel.";
                    EneterTrace.Error(anError);
                    throw new InvalidOperationException(anError);
                }

                try
                {
                    object aResponseMessage = mySerializer.ForResponseReceiver(responseReceiverId).Serialize<_ResponseType>(responseMessage);
                    AttachedDuplexInputChannel.SendResponseMessage(responseReceiverId, aResponseMessage);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.FailedToSendResponseMessage, err);
                    throw;
                }
            }
        }


        protected override void OnRequestMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (MessageReceived == null)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.NobodySubscribedForMessage);
                    return;
                }

                TypedRequestReceivedEventArgs<_RequestType> aRequestReceivedEventArgs = null;

                try
                {
                    _RequestType aRequestMessage = mySerializer.ForResponseReceiver(e.ResponseReceiverId).Deserialize<_RequestType>(e.Message);
                    aRequestReceivedEventArgs = new TypedRequestReceivedEventArgs<_RequestType>(e.ResponseReceiverId, e.SenderAddress, aRequestMessage);
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + "failed to deserialize the request message.", err);
                    aRequestReceivedEventArgs = new TypedRequestReceivedEventArgs<_RequestType>(e.ResponseReceiverId, e.SenderAddress, err);
                }

                try
                {
                    MessageReceived(this, aRequestReceivedEventArgs);
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                }
            }
        }


        protected override void OnResponseReceiverDisconnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (ResponseReceiverDisconnected != null)
                {
                    try
                    {
                        ResponseReceiverDisconnected(this, e);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }

        protected override void OnResponseReceiverConnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (ResponseReceiverConnected != null)
                {
                    try
                    {
                        ResponseReceiverConnected(this, e);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
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
