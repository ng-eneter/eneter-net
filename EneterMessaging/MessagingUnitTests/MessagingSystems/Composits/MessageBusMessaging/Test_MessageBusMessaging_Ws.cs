using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.WebSocketMessagingSystem;
using Eneter.Messaging.MessagingSystems.Composites.MessageBus;
using Eneter.Messaging.Diagnostic;

namespace Eneter.MessagingUnitTests.MessagingSystems.Composits.MessageBusMessaging
{
    [TestFixture]
    public class Test_MessageBusMessaging_Ws : BaseTester
    {
        [SetUp]
        public void Setup()
        {
            EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            //EneterTrace.TraceLog = new StreamWriter("d:/tracefile.txt");

            // Generate random number for the port.
            int aPort = RandomPortGenerator.GenerateInt();

            IMessagingSystemFactory anUnderlyingMessaging = new WebSocketMessagingSystemFactory();

            IDuplexInputChannel aMessageBusServiceInputChannel = anUnderlyingMessaging.CreateDuplexInputChannel("ws://[::1]:" + aPort + "/Clients/");
            IDuplexInputChannel aMessageBusClientInputChannel = anUnderlyingMessaging.CreateDuplexInputChannel("ws://[::1]:" + (aPort + 10) + "/Services/");
            myMessageBus = new MessageBusFactory().CreateMessageBus();
            myMessageBus.AttachDuplexInputChannels(aMessageBusServiceInputChannel, aMessageBusClientInputChannel);

            MessagingSystemFactory = new MessageBusMessagingFactory("ws://[::1]:" + aPort + "/Clients/", "ws://[::1]:" + (aPort + 10) + "/Services/", anUnderlyingMessaging);

            // Address of the service in the message bus.
            ChannelId = "Service1_Address";
        }

        [TearDown]
        public void TearDown()
        {
            if (myMessageBus != null)
            {
                myMessageBus.DetachDuplexInputChannels();
                myMessageBus = null;
            }
        }

        private IMessageBus myMessageBus;
    }
}
