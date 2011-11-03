
#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.Nodes.Bridge;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;
using System.IO;

namespace Eneter.MessagingUnitTests.Nodes.Bridge
{
    [TestFixture]
    public class Test_Bridge
    {
        [SetUp]
        public void Setup()
        {
            IBridgeFactory aBridgeFactory = new BridgeFactory();
            myBridge = aBridgeFactory.CreateBridge();
        }

        [Test]
        public void SendMessage()
        {
            IMessagingSystemFactory aMessagingSystemFactory = new SynchronousMessagingSystemFactory();
            IInputChannel anInputChannel = aMessagingSystemFactory.CreateInputChannel("Channel1");
            IOutputChannel anOutputChannel = aMessagingSystemFactory.CreateOutputChannel("Channel1");

            string aReceivedMessage = "";
            anInputChannel.MessageReceived += (x, y) =>
                {
                    aReceivedMessage = (string)y.Message;
                };
            anInputChannel.StartListening();

            myBridge.AttachOutputChannel(anOutputChannel);

            // Prepare input data
            MemoryStream aMemStream = new MemoryStream();
            BinaryWriter aBinWriter = new BinaryWriter(aMemStream);
            aBinWriter.Write((byte)4); // for string
            aBinWriter.Write("Hello World");

            // Send the message via the Bridge
            aMemStream.Position = 0;
            myBridge.SendMessage(aMemStream);

            Assert.AreEqual("Hello World", aReceivedMessage);
        }

        private IBridge myBridge;
    }
}

#endif