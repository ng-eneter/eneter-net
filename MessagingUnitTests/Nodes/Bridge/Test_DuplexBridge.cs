
#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.Nodes.Bridge;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;
using Eneter.Messaging.EndPoints.TypedMessages;
using System.IO;
using Eneter.Messaging.DataProcessing.Streaming;
using Eneter.Messaging.DataProcessing.Serializing;

namespace Eneter.MessagingUnitTests.Nodes.Bridge
{
    [TestFixture]
    public class Test_DuplexBridge
    {
        [SetUp]
        public void Setup()
        {
            myMessagingSystem = new SynchronousMessagingSystemFactory();

            IBridgeFactory aBridgeFactory = new BridgeFactory();
            myBridge = aBridgeFactory.CreateDuplexBridge(myMessagingSystem, "Channel1");
        }

        [Test]
        public void RequestResponse()
        {
            IDuplexInputChannel anInputChannel = myMessagingSystem.CreateDuplexInputChannel("Channel1");
            
            IDuplexTypedMessagesFactory aTypedRequestResponseFactory = new DuplexTypedMessagesFactory();
            IDuplexTypedMessageReceiver<int, int> aTypedResponser = aTypedRequestResponseFactory.CreateDuplexTypedMessageReceiver<int, int>();

            TypedRequestReceivedEventArgs<int> aReceivedRequest = null;
            aTypedResponser.MessageReceived += (x, y) =>
                {
                    aReceivedRequest = y;
                };

            ResponseReceiverEventArgs aReceivedConnection = null;
            aTypedResponser.ResponseReceiverConnected += (x, y) =>
                {
                    aReceivedConnection = y;
                };

            ResponseReceiverEventArgs aReceivedDisconnection = null;
            aTypedResponser.ResponseReceiverDisconnected += (x, y) =>
                {
                    aReceivedDisconnection = y;
                };

            aTypedResponser.AttachDuplexInputChannel(anInputChannel);
            

            // Prepare the connection message
            MemoryStream aConnectionStream = new MemoryStream();
            MessageStreamer.WriteOpenConnectionMessage(aConnectionStream, "ResponseReceiver 1");
            aConnectionStream.Position = 0;

            // Send the connection message via the bridge
            myBridge.ProcessRequestResponse(aConnectionStream, null);

            Assert.AreNotEqual("", aReceivedConnection.ResponseReceiverId);


            // Send the message
            ISerializer aSerializer = new XmlStringSerializer();
            object aMessage = aSerializer.Serialize<int>(100);
            MemoryStream aMessageStream = new MemoryStream();
            MessageStreamer.WriteRequestMessage(aMessageStream, "ResponseReceiver 1", aMessage);
            aMessageStream.Position = 0;

            // Send the message via the bridge
            myBridge.ProcessRequestResponse(aMessageStream, null);

            Assert.AreEqual(100, aReceivedRequest.RequestMessage);


            // Send response 1
            aTypedResponser.SendResponseMessage(aReceivedConnection.ResponseReceiverId, 501);

            // Send response 2
            aTypedResponser.SendResponseMessage(aReceivedConnection.ResponseReceiverId, 502);

            // Send response 3
            aTypedResponser.SendResponseMessage(aReceivedConnection.ResponseReceiverId, 503);


            // Pull responses from the bridge
            MemoryStream aPullRequestStream = new MemoryStream();
            MessageStreamer.WritePollResponseMessage(aPullRequestStream, "ResponseReceiver 1");
            aPullRequestStream.Position = 0;
            
            MemoryStream aPulledMessagesStream = new MemoryStream();

            myBridge.ProcessRequestResponse(aPullRequestStream, aPulledMessagesStream);

            List<int> aReceivedNumbers = new List<int>();
            aPulledMessagesStream.Position = 0;
            while (aPulledMessagesStream.Position < aPulledMessagesStream.Length)
            {
                object aRespondedMessage = MessageStreamer.ReadMessage(aPulledMessagesStream);

                Assert.IsNotNull(aRespondedMessage);

                int aNumber = aSerializer.Deserialize<int>(aRespondedMessage);

                aReceivedNumbers.Add(aNumber);
            }

            Assert.AreEqual(3, aReceivedNumbers.Count);
            Assert.AreEqual(501, aReceivedNumbers[0]);
            Assert.AreEqual(502, aReceivedNumbers[1]);
            Assert.AreEqual(503, aReceivedNumbers[2]);


            // Disconnect
            MemoryStream aDisconnectStream = new MemoryStream();
            MessageStreamer.WriteCloseConnectionMessage(aDisconnectStream, "ResponseReceiver 1");
            aDisconnectStream.Position = 0;

            myBridge.ProcessRequestResponse(aDisconnectStream, null);

            Assert.AreEqual(aReceivedConnection.ResponseReceiverId, aReceivedDisconnection.ResponseReceiverId);
        }


        private IMessagingSystemFactory myMessagingSystem;
        private IDuplexBridge myBridge;
    }
}


#endif