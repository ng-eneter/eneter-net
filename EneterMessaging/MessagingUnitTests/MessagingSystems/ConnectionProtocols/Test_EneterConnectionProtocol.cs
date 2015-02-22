using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using System.IO;

namespace Eneter.MessagingUnitTests.MessagingSystems.ConnectionProtocols
{
    [TestFixture]
    public class Test_EneterConnectionProtocol
    {
        [Test]
        public void WriteOpenConnectionRequest()
        {
            IProtocolFormatter aProtocolFormater = new EneterProtocolFormatter();

            byte[] anOpenConnectionMessage = (byte[])aProtocolFormater.EncodeOpenConnectionMessage("ResponseReceiver_1");

            ProtocolMessage aProtocolMessage = aProtocolFormater.DecodeMessage(new MemoryStream(anOpenConnectionMessage));

            Assert.AreEqual(EProtocolMessageType.OpenConnectionRequest, aProtocolMessage.MessageType);
            Assert.AreEqual("ResponseReceiver_1", aProtocolMessage.ResponseReceiverId);
            Assert.IsNull(aProtocolMessage.Message);
        }

        [Test]
        public void WriteCloseConnectionRequest()
        {
            IProtocolFormatter aProtocolFormater = new EneterProtocolFormatter();

            byte[] anCloseConnectionMessage = (byte[])aProtocolFormater.EncodeCloseConnectionMessage("ResponseReceiver_1");

            ProtocolMessage aProtocolMessage = aProtocolFormater.DecodeMessage(new MemoryStream(anCloseConnectionMessage));

            Assert.AreEqual(EProtocolMessageType.CloseConnectionRequest, aProtocolMessage.MessageType);
            Assert.AreEqual("ResponseReceiver_1", aProtocolMessage.ResponseReceiverId);
            Assert.IsNull(aProtocolMessage.Message);
        }

        [Test]
        public void WriteRequestMessage()
        {
            IProtocolFormatter aProtocolFormater = new EneterProtocolFormatter();

            String aMessage = "Hello";
            byte[] aRequestMessage = (byte[])aProtocolFormater.EncodeMessage("ResponseReceiver_1", aMessage);

            ProtocolMessage aProtocolMessage = aProtocolFormater.DecodeMessage(new MemoryStream(aRequestMessage));

            Assert.AreEqual(EProtocolMessageType.MessageReceived, aProtocolMessage.MessageType);
            Assert.AreEqual("ResponseReceiver_1", aProtocolMessage.ResponseReceiverId);
            Assert.AreEqual(aMessage, aProtocolMessage.Message);
        }
    }
}
