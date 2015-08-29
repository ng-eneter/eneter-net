using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.EndPoints.TypedMessages;
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
    internal class HolePunchingOutputChannel : IDuplexOutputChannel
    {
        public event EventHandler<DuplexChannelEventArgs> ConnectionOpened;

        public event EventHandler<DuplexChannelEventArgs> ConnectionClosed;

        public event EventHandler<DuplexChannelMessageEventArgs> ResponseMessageReceived;


        public HolePunchingOutputChannel(IMessagingSystemFactory outputChannel,
            string rendezvousId, ISerializer serializer, IDuplexOutputChannel rendezvousOutputChannel)
        {
            using (EneterTrace.Entering())
            {
            }
        }


        public string ChannelId { get; private set; }
        

        public string ResponseReceiverId
        {
            get { throw new NotImplementedException(); }
        }

        public void SendMessage(object message)
        {
            throw new NotImplementedException();
        }

        public void OpenConnection()
        {
            throw new NotImplementedException();
        }

        public void CloseConnection()
        {
            throw new NotImplementedException();
        }

        public bool IsConnected
        {
            get { throw new NotImplementedException(); }
        }

        public IThreadDispatcher Dispatcher
        {
            get { throw new NotImplementedException(); }
        }




        public void OpenConnection(Action<MessageContext> responseMessageHandler)
        {
            using (EneterTrace.Entering())
            {
                myRendezvousSender.AttachDuplexOutputChannel(myRendezvousOutputChannel);
                RendezvousMessage aRequest = new RendezvousMessage();
                aRequest.MessageType = ERendezvousMessage.GetAddressRequest;
                aRequest.MessageData = myRendezvousId;
                RendezvousMessage aResponse = myRendezvousSender.SendRequestMessage(aRequest);
                myRendezvousSender.DetachDuplexOutputChannel();

                //myOutputConnector.
            }
        }

        


        private IDuplexOutputChannel myOutputChannel;

        private string myRendezvousId;
        private IDuplexOutputChannel myRendezvousOutputChannel;
        private ISyncDuplexTypedMessageSender<RendezvousMessage, RendezvousMessage> myRendezvousSender;

        

        
    }
}
