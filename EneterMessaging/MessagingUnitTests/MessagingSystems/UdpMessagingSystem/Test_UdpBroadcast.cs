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
    public class Test_UdpBroadcast
    {
        [Test]
        public void BroadcastFromClientToAllServices()
        {
            UdpMessagingSystemFactory aMessaging = new UdpMessagingSystemFactory(new EasyProtocolFormatter())
            {
                IsSessionless = true,
                ReuseAddress = true
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

            IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("udp://255.255.255.255:8095/", "udp://127.0.0.1/");

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


        [Test]
        public void BroadcastFromServiceToAllClients()
        {
            UdpMessagingSystemFactory aMessaging = new UdpMessagingSystemFactory(new EasyProtocolFormatter())
            {
                IsSessionless = true,
                ReuseAddress = true
            };

            ManualResetEvent aMessage1Received = new ManualResetEvent(false);
            string aReceivedMessage1 = null;
            IDuplexOutputChannel anOutputChannel1 = aMessaging.CreateDuplexOutputChannel("udp://127.0.0.1:8090/", "udp://127.0.0.1:8092/");
            anOutputChannel1.ResponseMessageReceived += (x, y) =>
            {
                aReceivedMessage1 = (string)y.Message;
                aMessage1Received.Set();
            };

            ManualResetEvent aMessage2Received = new ManualResetEvent(false);
            string aReceivedMessage2 = null;
            IDuplexOutputChannel anOutputChannel2 = aMessaging.CreateDuplexOutputChannel("udp://127.0.0.1:8090/", "udp://127.0.0.1:8092/");
            anOutputChannel2.ResponseMessageReceived += (x, y) =>
            {
                aReceivedMessage2 = (string)y.Message;
                aMessage2Received.Set();
            };

            IDuplexInputChannel anInputChannel = aMessaging.CreateDuplexInputChannel("udp://127.0.0.1:8090/");

            try
            {
                anInputChannel.StartListening();

                anOutputChannel1.OpenConnection();
                anOutputChannel2.OpenConnection();

                anInputChannel.SendResponseMessage("udp://255.255.255.255:8092/", "Hello");

                aMessage1Received.WaitIfNotDebugging(1000);
                aMessage2Received.WaitIfNotDebugging(1000);
            }
            finally
            {
                anInputChannel.StopListening();

                anOutputChannel1.CloseConnection();
                anOutputChannel2.CloseConnection();
            }

            Assert.AreEqual("Hello", aReceivedMessage1);
            Assert.AreEqual("Hello", aReceivedMessage2);
        }
    }
}
