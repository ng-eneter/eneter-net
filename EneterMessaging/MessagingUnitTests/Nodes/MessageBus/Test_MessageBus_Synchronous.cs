using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;
using Eneter.Messaging.Nodes.Broker;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.Nodes.MessageBus;

namespace Eneter.MessagingUnitTests.Nodes.MessageBus
{
    [TestFixture]
    public class Test_MessageBus_Synchronous : MessageBusBaseTester
    {
        [SetUp]
        public void Setup()
        {
            IMessagingSystemFactory anUnderlyingMessaging = new SynchronousMessagingSystemFactory();

            // Create the broker.
            IDuplexBrokerFactory aBrokerFactory = new DuplexBrokerFactory();
            myBroker = aBrokerFactory.CreateBroker();
            myBroker.AttachDuplexInputChannel(anUnderlyingMessaging.CreateDuplexInputChannel("BrokerAddress"));

            MessagingSystemFactory = new MessageBusMessagingFactory("BrokerAddress", anUnderlyingMessaging);


            ChannelId = "Service1_Address";
        }

        [TearDown]
        public void TearDown()
        {
            if (myBroker != null)
            {
                myBroker.DetachDuplexInputChannel();
                myBroker = null;
            }
        }


        private IDuplexBroker myBroker;
    }
}
