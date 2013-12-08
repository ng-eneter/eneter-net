using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;
using Eneter.Messaging.MessagingSystems.Composites.MonitoredMessagingComposit;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using System.Threading;

namespace Eneter.MessagingUnitTests.MessagingSystems.Composits.BufferedMessaging
{
    [TestFixture]
    public class Test_MonitoredMessaging_Sync
    {
        [SetUp]
        public void Setup()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            //EneterTrace.TraceLog = new StreamWriter("d:/tracefile.txt");

            ChannelId = "Channel_1";
            UnderlyingMessaging = new SynchronousMessagingSystemFactory();
            ISerializer aSerializer = new XmlStringSerializer();
            TimeSpan aPingFrequency = TimeSpan.FromMilliseconds(50);
            TimeSpan aPingResponseTimeout = TimeSpan.FromMilliseconds(500);
            MessagingSystem = new MonitoredMessagingFactory(UnderlyingMessaging, aSerializer, aPingFrequency, aPingResponseTimeout);
            ConnectionInterruptionFrequency = 5;
        }


        [Test]
        public void A01_SimpleRequestResponse()
        {
            IDuplexOutputChannel aDuplexOutputChannel = MessagingSystem.CreateDuplexOutputChannel(ChannelId);
            IDuplexInputChannel aDuplexInputChannel = MessagingSystem.CreateDuplexInputChannel(ChannelId);


            // Received messages.
            List<int> aReceivedMessages = new List<int>();
            aDuplexInputChannel.MessageReceived += (x, y) =>
            {
                //EneterTrace.Info("Message Received");
                lock (aReceivedMessages)
                {
                    string aReceivedMessage = y.Message as string;

                    int k = int.Parse(aReceivedMessage);
                    aReceivedMessages.Add(k);
                    k += 1000;

                    aDuplexInputChannel.SendResponseMessage(y.ResponseReceiverId, k.ToString());
                }
            };

            // Received response messages.
            List<int> aReceivedResponseMessages = new List<int>();
            AutoResetEvent anAllMessagesProcessedEvent = new AutoResetEvent(false);
            aDuplexOutputChannel.ResponseMessageReceived += (x, y) =>
            {
                //EneterTrace.Info("Response Received");
                lock (aReceivedResponseMessages)
                {
                    string aReceivedMessage = y.Message as string;

                    int k = int.Parse(aReceivedMessage);
                    aReceivedResponseMessages.Add(k);

                    if (k == 1019)
                    {
                        anAllMessagesProcessedEvent.Set();
                    }
                }
            };


            try
            {
                aDuplexInputChannel.StartListening();
                aDuplexOutputChannel.OpenConnection();

                for (int i = 0; i < 20; ++i)
                {
                    aDuplexOutputChannel.SendMessage(i.ToString());
                }

                // Wait untill all messages are processed.
                anAllMessagesProcessedEvent.WaitOne();

            }
            finally
            {
                aDuplexOutputChannel.CloseConnection();
                aDuplexInputChannel.StopListening();
            }

            aReceivedMessages.Sort();
            Assert.AreEqual(20, aReceivedMessages.Count);
            for (int i = 0; i < 20; ++i)
            {
                Assert.AreEqual(i, aReceivedMessages[i]);
            }

            aReceivedResponseMessages.Sort();
            Assert.AreEqual(20, aReceivedResponseMessages.Count);
            for (int i = 0; i < 20; ++i)
            {
                Assert.AreEqual(i + 1000, aReceivedResponseMessages[i]);
            }


        }


        protected string ChannelId { get; set; }
        protected IMessagingSystemFactory UnderlyingMessaging { get; set; }
        protected IMessagingSystemFactory MessagingSystem { get; set; }
        protected int ConnectionInterruptionFrequency { get; set; }
    }
}
