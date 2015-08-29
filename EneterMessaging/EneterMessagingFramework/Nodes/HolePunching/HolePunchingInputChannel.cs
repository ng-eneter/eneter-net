using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.EndPoints.TypedMessages;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;
using Eneter.Messaging.Threading.Dispatching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eneter.Messaging.Nodes.HolePunching
{
    internal class HolePunchingInputChannel : IDuplexInputChannel
    {
        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;
        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;
        public event EventHandler<DuplexChannelMessageEventArgs> MessageReceived;

        public HolePunchingInputChannel(IDuplexInputChannel inputChannel,
            string rendezvousId, ISerializer serializer, IDuplexOutputChannel rendezvousOutputChannel)
        {
            using (EneterTrace.Entering())
            {
                myInputChannel = inputChannel;
                myInputChannel.MessageReceived += OnMessageReceived;
                myInputChannel.ResponseReceiverConnected += OnResponseReceiverConnected;
                myInputChannel.ResponseReceiverDisconnected += OnResponseReceiverDisconnected;

                ChannelId = rendezvousId;
                myRendezvousOutputChannel = rendezvousOutputChannel;

                IDuplexTypedMessagesFactory aFactory = new DuplexTypedMessagesFactory(serializer);
                myRendezvousSender = aFactory.CreateDuplexTypedMessageSender<RendezvousMessage, RendezvousMessage>();
            }
        }

        public string ChannelId { get; private set; }
        

        public void StartListening()
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myListeningManipulatorLock))
                {
                    myInputChannel.StartListening();

                    myRendezvousSender.AttachDuplexOutputChannel(myRendezvousOutputChannel);

                    RendezvousMessage aRequest = new RendezvousMessage();
                    aRequest.MessageType = ERendezvousMessage.RegisterRequest;
                    aRequest.MessageData = ChannelId;
                    myRendezvousSender.SendRequestMessage(aRequest);
                }
            }
        }

        public void StopListening()
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myListeningManipulatorLock))
                {
                    myRendezvousSender.DetachDuplexOutputChannel();

                    myInputChannel.StopListening();
                }
            }
        }

        public bool IsListening
        {
            get
            {
                using (ThreadLock.Lock(myListeningManipulatorLock))
                {
                    return myRendezvousSender.IsDuplexOutputChannelAttached && myRendezvousSender.AttachedDuplexOutputChannel.IsConnected && myInputChannel.IsListening;
                }
            }
        }

        public void DisconnectResponseReceiver(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                myInputChannel.DisconnectResponseReceiver(responseReceiverId);
            }
        }

        public IThreadDispatcher Dispatcher
        {
            get { return myInputChannel.Dispatcher; }
        }

        public void SendResponseMessage(string responseReceiverId, object message)
        {
            using (EneterTrace.Entering())
            {
                myInputChannel.SendResponseMessage(responseReceiverId, message);
            }
        }


        private void OnMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                Notify<DuplexChannelMessageEventArgs>(MessageReceived, e, true);
            }
        }

        private void OnResponseReceiverConnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                Notify<ResponseReceiverEventArgs>(ResponseReceiverConnected, e, false);
            }
        }

        private void OnResponseReceiverDisconnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                Notify<ResponseReceiverEventArgs>(ResponseReceiverDisconnected, e, false);
            }
        }

        private void Notify<T>(EventHandler<T> handler, T eventArgs, bool isNobodySubscribedWarning)
            where T : EventArgs
        {
            using (EneterTrace.Entering())
            {
                if (handler != null)
                {
                    try
                    {
                        handler(this, eventArgs);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
                else if (isNobodySubscribedWarning)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.NobodySubscribedForMessage);
                }
            }
        }


        private object myListeningManipulatorLock = new object();
        private IDuplexInputChannel myInputChannel;

        private IDuplexOutputChannel myRendezvousOutputChannel;
        private IDuplexTypedMessageSender<RendezvousMessage, RendezvousMessage> myRendezvousSender;


        private string TracedObject { get { return GetType().Name + " "; } }
    }
}
