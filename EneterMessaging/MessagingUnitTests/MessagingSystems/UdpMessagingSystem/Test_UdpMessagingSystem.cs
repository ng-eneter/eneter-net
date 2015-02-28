using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.UdpMessagingSystem;
using Eneter.Messaging.Diagnostic;

namespace Eneter.MessagingUnitTests.MessagingSystems.UdpMessagingSystem
{
    [TestFixture]
    public class Test_UdpMessagingSystem : BaseTester
    {
        [SetUp]
        public void Setup()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            //EneterTrace.TraceLog = new StreamWriter("d:/tracefile.txt");

            // Generate random number for the port.
            Random aRnd = new Random();
            int aPort = aRnd.Next(8000, 9000);

            MessagingSystemFactory = new UdpMessagingSystemFactory();
            ChannelId = "udp://127.0.0.1:" + aPort + "/";
        }

        // Following tests does not have a sense for UDP.
        // Max message size in UDP is 65536.
        // Some disconnections e.g. input channel suddenly stopps are not detectable.


        [Ignore]
        [Test]
        public override void Duplex_03_Send100_10MB()
        {
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
    }
}
