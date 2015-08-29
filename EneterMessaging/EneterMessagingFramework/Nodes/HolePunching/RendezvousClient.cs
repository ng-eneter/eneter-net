using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.EndPoints.TypedMessages;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eneter.Messaging.Nodes.HolePunching
{
    public class RendezvousClient : IAttachableDuplexOutputChannel
    {
        public RendezvousClient(ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                IDuplexTypedMessagesFactory aFactory = new DuplexTypedMessagesFactory(serializer);
                myRendezvousSender = aFactory.CreateSyncDuplexTypedMessageSender<RendezvousMessage, RendezvousMessage>();
            }
        }

        public string Register(string rendezvousId)
        {
            using (EneterTrace.Entering())
            {
                RendezvousMessage aRequest = new RendezvousMessage();
                aRequest.MessageType = ERendezvousMessage.RegisterRequest;
                aRequest.MessageData = rendezvousId;

                RendezvousMessage aResponse = myRendezvousSender.SendRequestMessage(aRequest);

                if (aResponse.MessageType != ERendezvousMessage.AddressResponse)
                {
                    throw new InvalidOperationException(TracedObject + "failed to register in rendezvous service.");
                }

                return aResponse.MessageData;
            }
        }

        public string GetAddress(string rendezvousId)
        {
            using (EneterTrace.Entering())
            {
                RendezvousMessage aRequest = new RendezvousMessage();
                aRequest.MessageType = ERendezvousMessage.GetAddressRequest;
                aRequest.MessageData = rendezvousId;

                RendezvousMessage aResponse = myRendezvousSender.SendRequestMessage(aRequest);

                if (aResponse.MessageType != ERendezvousMessage.AddressResponse)
                {
                    throw new InvalidOperationException(TracedObject + "failed to register in rendezvous service.");
                }

                return aResponse.MessageData;
            }
        }
        

        public void AttachDuplexOutputChannel(IDuplexOutputChannel duplexOutputChannel)
        {
            using (EneterTrace.Entering())
            {
                myRendezvousSender.AttachDuplexOutputChannel(duplexOutputChannel);
            }
        }

        public void DetachDuplexOutputChannel()
        {
            using (EneterTrace.Entering())
            {
                myRendezvousSender.DetachDuplexOutputChannel();
            }
        }

        public bool IsDuplexOutputChannelAttached
        {
            get { return myRendezvousSender.IsDuplexOutputChannelAttached; }
        }

        public IDuplexOutputChannel AttachedDuplexOutputChannel
        {
            get { return myRendezvousSender.AttachedDuplexOutputChannel; }
        }


        private ISyncDuplexTypedMessageSender<RendezvousMessage, RendezvousMessage> myRendezvousSender;

        private string TracedObject { get { return GetType().Name + " "; } }
    }
}
