using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SharedMemoryMessagingSystem;
using Eneter.Messaging.MessagingSystems.Composites.BusMessaging;

namespace Eneter.MessagingUnitTests.MessagingSystems.Composits.MessageBusMessaging
{
    [TestFixture]
    public class Test_TestMessaeBusMessaging_SharedMemory : BaseTester
    {
        [SetUp]
        public void Setup()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            //EneterTrace.TraceLog = new StreamWriter("d:/tracefile.txt");

            // Generate random number for the port.
            int aPort1 = RandomPortGenerator.GenerateInt();
            int aPort2 = aPort1 + 10;

            IMessagingSystemFactory anUnderlyingMessaging = new SharedMemoryMessagingSystemFactory();

            IDuplexInputChannel aMessageBusServiceInputChannel = anUnderlyingMessaging.CreateDuplexInputChannel("MyServicesAddress");
            IDuplexInputChannel aMessageBusClientInputChannel = anUnderlyingMessaging.CreateDuplexInputChannel("MyClientsAddress");
            myMessageBus = new MessageBusFactory().CreateMessageBus();
            myMessageBus.AttachDuplexInputChannels(aMessageBusServiceInputChannel, aMessageBusClientInputChannel);

            MessagingSystemFactory = new MessageBusMessagingFactory("MyServicesAddress", "MyClientsAddress", anUnderlyingMessaging);

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
