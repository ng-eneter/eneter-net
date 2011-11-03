using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    internal class ReliableDuplexTypedMessageReceiver<_ResponseType, _RequestType> : AttachableReliableInputChannelBase, IReliableTypedMessageReceiver<_ResponseType, _RequestType>
    {
        public event EventHandler<TypedRequestReceivedEventArgs<_RequestType>> MessageReceived;

        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;

        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;


        public ReliableDuplexTypedMessageReceiver(ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                mySerializer = serializer;
            }
        }

        public string SendResponseMessage(string responseReceiverId, _ResponseType responseMessage)
        {
            using (EneterTrace.Entering())
            {
                if (AttachedReliableInputChannel == null)
                {
                    string anError = TracedObject + "failed to send the response message because it is not attached to any duplex input channel.";
                    EneterTrace.Error(anError);
                    throw new InvalidOperationException(anError);
                }

                try
                {
                    object aResponseMessage = mySerializer.Serialize<_ResponseType>(responseMessage);
                    return AttachedReliableInputChannel.SendResponseMessage(responseReceiverId, aResponseMessage);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.SendResponseFailure, err);
                    throw;
                }
            }
        }


        protected override void OnMessageReceived(object sender, DuplexChannelMessageEventArgs e)
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
                    _RequestType aRequestMessage = mySerializer.Deserialize<_RequestType>(e.Message);
                    aRequestReceivedEventArgs = new TypedRequestReceivedEventArgs<_RequestType>(e.ResponseReceiverId, aRequestMessage);
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + "failed to deserialize the request message.", err);
                    aRequestReceivedEventArgs = new TypedRequestReceivedEventArgs<_RequestType>(e.ResponseReceiverId, err);
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
                string aDuplexInputChannelId = (AttachedDuplexInputChannel != null) ? AttachedDuplexInputChannel.ChannelId : "";
                return "The TypedResponser<" + typeof(_ResponseType).Name + ", " + typeof(_RequestType).Name + "> atached to the duplex input channel '" + aDuplexInputChannelId + "' ";
            }
        }
    }
}
