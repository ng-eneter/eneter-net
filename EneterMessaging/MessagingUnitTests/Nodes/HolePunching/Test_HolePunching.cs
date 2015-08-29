using Eneter.Messaging.DataProcessing.Serializing;
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
    public class Test_HolePunching : BaseTester
    {
        [SetUp]
        public void Setup()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            //EneterTrace.TraceLog = new StreamWriter("d:/tracefile.txt");
            //EneterTrace.StartProfiler();

            // Generate random number for the port.
            string aPort = RandomPortGenerator.Generate();

            MessagingSystemFactory = new TcpMessagingSystemFactory();


            IDuplexInputChannel aServiceInputChannel = MessagingSystemFactory.CreateDuplexInputChannel("tcp://127.0.0.1:8092/");
            RendezvousService aService = new RendezvousService(new XmlStringSerializer());
            aService.AttachDuplexInputChannel(aServiceInputChannel);

            IDuplexOutputChannel aClientOutputChannel = MessagingSystemFactory.CreateDuplexOutputChannel("tcp://127.0.0.1:8092/");
            RendezvousClient aClient = new RendezvousClient(new XmlStringSerializer());
            aClient.AttachDuplexOutputChannel(aClientOutputChannel);
            string ipAddressAndPort = aClient.Register("hello");

            ChannelId = "tcp://" + ipAddressAndPort + "/";
        }
    }
}
