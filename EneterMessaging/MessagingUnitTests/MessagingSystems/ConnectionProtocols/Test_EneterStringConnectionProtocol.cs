using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;

namespace Eneter.MessagingUnitTests.MessagingSystems.ConnectionProtocols
{
    [TestFixture]
    public class Test_EneterStringConnectionProtocol
    {
        [Test]
        public void WriteOpenConnectionRequest()
        {
            IProtocolFormatter aProtocolFormater = new EneterStringProtocolFormatter();

            string anOpenConnectionMessage = (string)aProtocolFormater.EncodeOpenConnectionMessage("ResponseReceiver_1");

            ProtocolMessage aProtocolMessage = aProtocolFormater.DecodeMessage(anOpenConnectionMessage);

            Assert.AreEqual(EProtocolMessageType.OpenConnectionRequest, aProtocolMessage.MessageType);
            Assert.AreEqual("ResponseReceiver_1", aProtocolMessage.ResponseReceiverId);
            Assert.IsNull(aProtocolMessage.Message);
        }

        [Test]
        public void WriteCloseConnectionRequest()
        {
            IProtocolFormatter aProtocolFormater = new EneterStringProtocolFormatter();

            string anOpenConnectionMessage = (string)aProtocolFormater.EncodeCloseConnectionMessage("ResponseReceiver_1");

            ProtocolMessage aProtocolMessage = aProtocolFormater.DecodeMessage(anOpenConnectionMessage);

            Assert.AreEqual(EProtocolMessageType.CloseConnectionRequest, aProtocolMessage.MessageType);
            Assert.AreEqual("ResponseReceiver_1", aProtocolMessage.ResponseReceiverId);
            Assert.IsNull(aProtocolMessage.Message);
        }

        [Test]
        public void WriteRequestMessage()
        {
            IProtocolFormatter aProtocolFormater = new EneterStringProtocolFormatter();

            string aMessage = "Hello";
            string anOpenConnectionMessage = (string)aProtocolFormater.EncodeMessage("ResponseReceiver_1", aMessage);

            ProtocolMessage aProtocolMessage = aProtocolFormater.DecodeMessage(anOpenConnectionMessage);

            Assert.AreEqual(EProtocolMessageType.MessageReceived, aProtocolMessage.MessageType);
            Assert.AreEqual("ResponseReceiver_1", aProtocolMessage.ResponseReceiverId);
            Assert.AreEqual(aMessage, aProtocolMessage.Message);
        }
    }
}
