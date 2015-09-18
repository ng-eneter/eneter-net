using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
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
    public class Test_TcpHolePunching_ExactPort : BaseTester
    {
        [SetUp]
        public void Setup()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            //EneterTrace.TraceLog = new StreamWriter("d:/tracefile.txt");
            //EneterTrace.StartProfiler();

            // Generate random number for the port.
            int aPort = RandomPortGenerator.GenerateInt();

            MessagingSystemFactory = new TcpMessagingSystemFactory()
            {
                ReuseAddress = true
            };

            TcpMessagingSystemFactory aRendezvousServiceMessaging = new TcpMessagingSystemFactory();
            TcpMessagingSystemFactory aRendezvousClientMessaging = new TcpMessagingSystemFactory()
            {
                ReuseAddress = true
            };

            IRendezvousFactory aRendezvousFactory = new RendezvousFactory();

            IDuplexInputChannel aServiceInputChannel = aRendezvousServiceMessaging.CreateDuplexInputChannel("tcp://127.0.0.1:8092/");
            myRendezvousService = aRendezvousFactory.CreateRendezvousService();
            myRendezvousService.AttachDuplexInputChannel(aServiceInputChannel);

            IDuplexOutputChannel aClientOutputChannel = aRendezvousClientMessaging.CreateDuplexOutputChannel("tcp://127.0.0.1:8092/", null, aPort);
            myRendezvousClient = aRendezvousFactory.CreateRendezvousClient();
            myRendezvousClient.AttachDuplexOutputChannel(aClientOutputChannel);
            string ipAddressAndPort = myRendezvousClient.Register("hello");

            ChannelId = "tcp://" + ipAddressAndPort + "/";
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
