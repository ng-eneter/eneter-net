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
    public class RendezvousService : IAttachableDuplexInputChannel
    {
        public RendezvousService(ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                DuplexTypedMessagesFactory aFactory = new DuplexTypedMessagesFactory(serializer);
                myReceiver = aFactory.CreateDuplexTypedMessageReceiver<RendezvousMessage, RendezvousMessage>();
                myReceiver.ResponseReceiverConnected += OnResponseReceiverConnected;
                myReceiver.ResponseReceiverDisconnected += OnResponseReceiverDisconnected;
                myReceiver.MessageReceived += OnMessageReceived;
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
            get { throw new NotImplementedException(); }
        }

        public IDuplexInputChannel AttachedDuplexInputChannel
        {
            get { throw new NotImplementedException(); }
        }


        private void OnResponseReceiverConnected(object sender, ResponseReceiverEventArgs e)
        {
        }

        private void OnResponseReceiverDisconnected(object sender, ResponseReceiverEventArgs e)
        {
        }

        private void OnMessageReceived(object sender, TypedRequestReceivedEventArgs<RendezvousMessage> e)
        {
            using (EneterTrace.Entering())
            {
                if (e.ReceivingError != null)
                {
                    EneterTrace.Warning(TracedObject + "could not process the request.", e.ReceivingError);
                    return;
                }

                if (e.RequestMessage.MessageType == ERendezvousMessage.RegisterRequest)
                {
                    Register(e.RequestMessage.MessageData, e.SenderAddress, e.ResponseReceiverId);
                }
                else if (e.RequestMessage.MessageType == ERendezvousMessage.GetAddressRequest)
                {
                    ProvideAddress(e.RequestMessage.MessageData, e.ResponseReceiverId);
                }
            }
        }

        private void Register(string rendezvousId, string ipAddressAndPort, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                if (string.IsNullOrEmpty(rendezvousId))
                {
                    EneterTrace.Error(TracedObject + "could not register empty string of rendezvousId.");
                    return;
                }

                if (string.IsNullOrEmpty(ipAddressAndPort))
                {
                    EneterTrace.Error(TracedObject + "could not register empty string of ipAddressAndPort.");
                    return;
                }

                using (ThreadLock.Lock(myRegisteredEndPoints))
                {
                    myRegisteredEndPoints[rendezvousId] = ipAddressAndPort;

                    RendezvousMessage aResponse = new RendezvousMessage();
                    aResponse.MessageType = ERendezvousMessage.AddressResponse;
                    aResponse.MessageData = ipAddressAndPort;
                    myReceiver.SendResponseMessage(responseReceiverId, aResponse);
                }
            }
        }

        private void ProvideAddress(string rendezvousId, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                string anIpAddressAndPort;
                using (ThreadLock.Lock(myRegisteredEndPoints))
                {
                    myRegisteredEndPoints.TryGetValue(rendezvousId, out anIpAddressAndPort);
                }

                if (!string.IsNullOrEmpty(anIpAddressAndPort))
                {
                    RendezvousMessage aResponse = new RendezvousMessage();
                    aResponse.MessageType = ERendezvousMessage.AddressResponse;
                    aResponse.MessageData = anIpAddressAndPort;
                    myReceiver.SendResponseMessage(responseReceiverId, aResponse);
                }
            }
        }


        private IDuplexTypedMessageReceiver<RendezvousMessage, RendezvousMessage> myReceiver;

        private Dictionary<string, string> myRegisteredEndPoints = new Dictionary<string, string>();

        private string TracedObject
        {
            get { return GetType().Name + " "; }
        }
    }
}
