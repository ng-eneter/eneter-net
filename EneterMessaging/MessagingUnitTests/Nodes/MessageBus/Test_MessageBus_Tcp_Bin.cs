
#if !COMPACT_FRAMEWORK

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.Nodes.Broker;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
using Eneter.Messaging.Nodes.MessageBus;
using Eneter.Messaging.DataProcessing.Serializing;

namespace Eneter.MessagingUnitTests.Nodes.MessageBus
{
    [TestFixture]
    public class Test_MessageBus_Tcp_Bin : MessageBusBaseTester
    {
        [SetUp]
        public void Setup()
        {
            IMessagingSystemFactory anUnderlyingMessaging = new TcpMessagingSystemFactory();

            ISerializer aSerializer = new BinarySerializer();

            // Create the broker.
            IDuplexBrokerFactory aBrokerFactory = new DuplexBrokerFactory(aSerializer);
            myBroker = aBrokerFactory.CreateBroker();
            myBroker.AttachDuplexInputChannel(anUnderlyingMessaging.CreateDuplexInputChannel("tcp://127.0.0.1:8034/"));

            MessagingSystemFactory = new MessageBusMessagingFactory("tcp://127.0.0.1:8034/", anUnderlyingMessaging, aSerializer);


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

#endif