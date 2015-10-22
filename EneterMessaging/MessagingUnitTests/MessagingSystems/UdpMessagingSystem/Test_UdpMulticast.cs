using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.UdpMessagingSystem;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Eneter.MessagingUnitTests.MessagingSystems.UdpMessagingSystem
{
    [TestFixture]
    public class Test_UdpMulticast
    {
        [Test]
        public void MulticastFromClientToServices()
        {
            UdpMessagingSystemFactory aMessaging = new UdpMessagingSystemFactory(new EasyProtocolFormatter())
            {
                IsSessionless = true,
                ReuseAddress = true,
                MulticastGroup = "234.1.2.3"
            };

            ManualResetEvent aMessage1Received = new ManualResetEvent(false);
            string aReceivedMessage1 = null;
            IDuplexInputChannel anInputChannel1 = aMessaging.CreateDuplexInputChannel("udp://127.0.0.1:8095/");
            anInputChannel1.MessageReceived += (x, y) =>
            {
                aReceivedMessage1 = (string)y.Message;
                aMessage1Received.Set();
            };

            ManualResetEvent aMessage2Received = new ManualResetEvent(false);
            string aReceivedMessage2 = null;
            IDuplexInputChannel anInputChannel2 = aMessaging.CreateDuplexInputChannel("udp://127.0.0.1:8095/");
            anInputChannel2.MessageReceived += (x, y) =>
            {
                aReceivedMessage2 = (string)y.Message;
                aMessage2Received.Set();
            };

            IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("udp://234.1.2.3:8095/", "udp://127.0.0.1:8095/");

            try
            {
                anInputChannel1.StartListening();
                anInputChannel2.StartListening();

                anOutputChannel.OpenConnection();

                anOutputChannel.SendMessage("Hello");

                aMessage1Received.WaitIfNotDebugging(1000);
                aMessage2Received.WaitIfNotDebugging(1000);
            }
            finally
            {
                anOutputChannel.CloseConnection();

                anInputChannel1.StopListening();
                anInputChannel2.StopListening();
            }

            Assert.AreEqual("Hello", aReceivedMessage1);
            Assert.AreEqual("Hello", aReceivedMessage2);
        }
    }
}
