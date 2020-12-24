



using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
namespace Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase
{
    internal class MessageContext
    {
        public MessageContext(ProtocolMessage message, string senderAddress)
        {
            ProtocolMessage = message;
            SenderAddress = senderAddress;
        }

        public ProtocolMessage ProtocolMessage { get; private set; }
        public string SenderAddress { get; private set; }
    }
}
