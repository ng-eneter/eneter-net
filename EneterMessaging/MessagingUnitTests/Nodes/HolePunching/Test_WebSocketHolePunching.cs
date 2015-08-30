using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.WebSocketMessagingSystem;
using Eneter.Messaging.Nodes.HolePunching;
using Eneter.MessagingUnitTests.MessagingSystems;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Eneter.MessagingUnitTests.Nodes.HolePunching
{
    [TestFixture]
    public class Test_WebSocketHolePunching : BaseTester
    {
        [SetUp]
        public void Setup()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            //EneterTrace.TraceLog = new StreamWriter("d:/tracefile.txt");
            //EneterTrace.StartProfiler();

            MessagingSystemFactory = new WebSocketMessagingSystemFactory()
            {
                ReuseAddress = true
            };

            IRendezvousFactory aRendezvousFactory = new RendezvousFactory();

            IDuplexInputChannel aServiceInputChannel = MessagingSystemFactory.CreateDuplexInputChannel("ws://127.0.0.1:8092/");
            myRendezvousService = aRendezvousFactory.CreateRendezvousService();
            myRendezvousService.AttachDuplexInputChannel(aServiceInputChannel);

            IDuplexOutputChannel aClientOutputChannel = MessagingSystemFactory.CreateDuplexOutputChannel("ws://127.0.0.1:8092/");
            myRendezvousClient = aRendezvousFactory.CreateRendezvousClient();
            myRendezvousClient.AttachDuplexOutputChannel(aClientOutputChannel);
            string ipAddressAndPort = myRendezvousClient.Register("hello");

            ChannelId = "ws://" + ipAddressAndPort + "/";
        }

        [TearDown]
        public void Clean()
        {
            if (myRendezvousClient != null)
            {
                myRendezvousClient.DetachDuplexOutputChannel();
            }

            if (myRendezvousService != null)
            {
                myRendezvousService.DetachDuplexInputChannel();
            }
        }

        private IRendezvousClient myRendezvousClient;
        private IRendezvousService myRendezvousService;
    }
}
