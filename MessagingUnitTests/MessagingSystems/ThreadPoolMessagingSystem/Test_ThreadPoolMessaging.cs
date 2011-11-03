using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.ThreadPoolMessagingSystem;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using System.Threading;

namespace Eneter.MessagingUnitTests.MessagingSystems.ThreadPoolMessagingSystem
{
    [TestFixture]
    public class Test_ThreadPoolMessaging : MessagingSystemBaseTester
    {
        [SetUp]
        public void Setup()
        {
            MessagingSystemFactory = new ThreadPoolMessagingSystemFactory();
        }

        [Test]
        public void SendMessages100000()
        {
            IOutputChannel anOutputChannel = MessagingSystemFactory.CreateOutputChannel(ChannelId);
            IInputChannel anInputChannel = MessagingSystemFactory.CreateInputChannel(ChannelId);

            ManualResetEvent aMessagesSentEvent = new ManualResetEvent(false);

            List<ChannelMessageEventArgs> aReceivedMessages = new List<ChannelMessageEventArgs>();
            anInputChannel.MessageReceived += (x, y) =>
            {
                // Some messaging system can have a parallel access therefore we must ensure
                // that results are put to the list synchronously.
                lock (aReceivedMessages)
                {
                    aReceivedMessages.Add(y);

                    // Release helper thread when all messages are received.
                    if (aReceivedMessages.Count == 100000)
                    {
                        aMessagesSentEvent.Set();
                    }
                }
            };

            try
            {
                anInputChannel.StartListening();

                // Send messages
                for (int i = 0; i < 100000; ++i)
                {
                    anOutputChannel.SendMessage("Message");
                }

                // Wait until all messages are processed.
                Assert.IsTrue(aMessagesSentEvent.WaitOne(3000));
            }
            finally
            {
                anInputChannel.StopListening();
            }

            aReceivedMessages.ForEach(x => Assert.AreEqual(ChannelId, x.ChannelId));
            aReceivedMessages.ForEach(x => Assert.AreEqual("Message", (string)x.Message));
        }
    }
}
