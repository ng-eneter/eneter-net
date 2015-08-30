using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.UdpMessagingSystem;
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
    public class Test_UdpHolePunching : BaseTester
    {
        [SetUp]
        public void Setup()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            //EneterTrace.TraceLog = new StreamWriter("d:/tracefile.txt");
            //EneterTrace.StartProfiler();

            MessagingSystemFactory = new UdpMessagingSystemFactory();

            IRendezvousFactory aRendezvousFactory = new RendezvousFactory();

            IDuplexInputChannel aServiceInputChannel = MessagingSystemFactory.CreateDuplexInputChannel("udp://127.0.0.1:8092/");
            myRendezvousService = aRendezvousFactory.CreateRendezvousService();
            myRendezvousService.AttachDuplexInputChannel(aServiceInputChannel);

            IDuplexOutputChannel aClientOutputChannel = MessagingSystemFactory.CreateDuplexOutputChannel("udp://127.0.0.1:8092/");
            myRendezvousClient = aRendezvousFactory.CreateRendezvousClient();
            myRendezvousClient.AttachDuplexOutputChannel(aClientOutputChannel);
            string ipAddressAndPort = myRendezvousClient.Register("hello");

            ChannelId = "udp://" + ipAddressAndPort + "/";
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

        [Ignore]
        [Test]
        public override void Duplex_03_Send1_10MB()
        {
        }

        [Ignore]
        [Test]
        public override void Duplex_07_OpenConnection_if_InputChannelNotStarted()
        {
        }

        [Ignore]
        [Test]
        public override void Duplex_09_StopListening_SendMessage()
        {
        }

        private IRendezvousClient myRendezvousClient;
        private IRendezvousService myRendezvousService;
    }
}
