using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    internal class ReliableDuplexTypedMessageSender<_ResponseType, _RequestType> : AttachableReliableOutputChannelBase, IReliableTypedMessageSender<_ResponseType, _RequestType>
    {
        public event EventHandler<TypedResponseReceivedEventArgs<_ResponseType>> ResponseReceived;


        public ReliableDuplexTypedMessageSender(ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                mySerializer = serializer;
            }
        }

        public string SendRequestMessage(_RequestType message)
        {
            using (EneterTrace.Entering())
            {
                if (AttachedReliableOutputChannel == null)
                {
                    string anError = TracedObject + "failed to send the request message because it is not attached to any duplex output channel.";
                    EneterTrace.Error(anError);
                    throw new InvalidOperationException(anError);
                }

                try
                {
                    object aRequestMessage = mySerializer.Serialize<_RequestType>(message);
                    return AttachedReliableOutputChannel.SendMessage(aRequestMessage);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.SendMessageFailure, err);
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
                    _ResponseType aResponseMessage = mySerializer.Deserialize<_ResponseType>(e.Message);
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


        private ISerializer mySerializer;

        protected override string TracedObject
        {
            get
            {
                string aDuplexOutputChannelId = (AttachedDuplexOutputChannel != null) ? AttachedDuplexOutputChannel.ChannelId : "";
                return "The TypedRequester<" + typeof(_ResponseType).Name + ", " + typeof(_RequestType).Name + "> atached to the duplex output channel '" + aDuplexOutputChannelId + "' ";
            }
        }
    }
}
