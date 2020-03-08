using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.UdpMessagingSystem;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using System.Threading;
using System.IO;

namespace Eneter.MessagingUnitTests.MessagingSystems.UdpMessagingSystem
{
    [TestFixture]
    public class Test_UdpMessagingSystem : BaseTester
    {
        [SetUp]
        public void Setup()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            //EneterTrace.TraceLog = new StreamWriter("c:/tmp/tracefile.txt");

            // Generate random number for the port.
            Random aRnd = new Random();
            int aPort = aRnd.Next(8000, 9000);

            MessagingSystemFactory = new UdpMessagingSystemFactory();
            ChannelId = "udp://127.0.0.1:" + aPort + "/";
        }

        // Following tests does not have a sense for UDP.
        // Max message size in UDP is 65536.
        // Some disconnections e.g. input channel suddenly stopps are not detectable.


        [Ignore("")]
        [Test]
        public override void Duplex_03_Send1_10MB()
        {
        }

        [Ignore("")]
        [Test]
        public override void Duplex_07_OpenConnection_if_InputChannelNotStarted()
        {
        }

        [Ignore("")]
        [Test]
        public override void Duplex_09_StopListening_SendMessage()
        {
        }

        [Test]
        public void TestPortAvailability()
        {
            IDuplexInputChannel anInputChannel1 = MessagingSystemFactory.CreateDuplexInputChannel("udp://[::1]:8045/");
            IDuplexInputChannel anInputChannel2 = MessagingSystemFactory.CreateDuplexInputChannel("udp://127.0.0.1:8045/");

            try
            {
                anInputChannel1.StartListening();
                anInputChannel2.StartListening();

                bool aResult = UdpMessagingSystemFactory.IsPortAvailableForUdpListening("tcp://[::1]:8045/");
                Assert.IsFalse(aResult);

                aResult = UdpMessagingSystemFactory.IsPortAvailableForUdpListening("tcp://0.0.0.0:8046/");
                Assert.IsTrue(aResult);
            }
            finally
            {
                anInputChannel1.StopListening();
                anInputChannel2.StopListening();
            }
        }

        [Test]
        public void MaxAmountOfConnections()
        {
            IMessagingSystemFactory aMessaging = new UdpMessagingSystemFactory()
            {
                MaxAmountOfConnections = 2
            };
            IDuplexOutputChannel anOutputChannel1 = aMessaging.CreateDuplexOutputChannel("udp://127.0.0.1:8049/");
            IDuplexOutputChannel anOutputChannel2 = aMessaging.CreateDuplexOutputChannel("udp://127.0.0.1:8049/");
            IDuplexOutputChannel anOutputChannel3 = aMessaging.CreateDuplexOutputChannel("udp://127.0.0.1:8049/");
            IDuplexInputChannel anInputChannel = aMessaging.CreateDuplexInputChannel("udp://127.0.0.1:8049/");

            try
            {
                ManualResetEvent aConnectionClosed = new ManualResetEvent(false);
                anOutputChannel3.ConnectionClosed += (x, y) =>
                {
                    EneterTrace.Info("Connection closed.");
                    aConnectionClosed.Set();
                };


                anInputChannel.StartListening();
                anOutputChannel1.OpenConnection();
                anOutputChannel2.OpenConnection();
                anOutputChannel3.OpenConnection();

                if (!aConnectionClosed.WaitOne(1000))
                {
                    Assert.Fail("Third connection was not closed.");
                }

                Assert.IsTrue(anOutputChannel1.IsConnected);
                Assert.IsTrue(anOutputChannel2.IsConnected);
                Assert.IsFalse(anOutputChannel3.IsConnected);
            }
            finally
            {
                anOutputChannel1.CloseConnection();
                anOutputChannel2.CloseConnection();
                anOutputChannel3.CloseConnection();
                anInputChannel.StopListening();
            }
        }
    }
}
