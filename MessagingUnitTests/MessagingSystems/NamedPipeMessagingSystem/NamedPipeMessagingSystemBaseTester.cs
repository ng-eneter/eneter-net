
#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Threading;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.NamedPipeMessagingSystem;

namespace Eneter.MessagingUnitTests.MessagingSystems.NamedPipeMessagingSystem
{
    public abstract class NamedPipeMessagingSystemBaseTester : MessagingSystemBaseTester
    {
        [Test]
        [ExpectedException(typeof(TimeoutException))]
        public override void A07_StopListening()
        {
            base.A07_StopListening();
        }

        [Test]
        public void B01_SendReceiveMessage_4threads_80000messages()
        {
            IInputChannel anInputChannel = MessagingSystemFactory.CreateInputChannel(ChannelId);
            IOutputChannel anOutputChannel = MessagingSystemFactory.CreateOutputChannel(ChannelId);

            // Event signalint the end of the message handling
            AutoResetEvent anAllMessagesHandledSignal = new AutoResetEvent(false);

            // Observe the input channel
            List<string> aReceivedMessages = new List<string>();
            anInputChannel.MessageReceived += (x, y) =>
            {
                aReceivedMessages.Add((string)y.Message);

                if (aReceivedMessages.Count == 20000 * 4)
                {
                    anAllMessagesHandledSignal.Set();
                }
            };

            try
            {
                List<Thread> aThreads = new List<Thread>();
                for (int i = 0; i < 4; ++i)
                {
                    Thread aThread = new Thread(() =>
                        {
                            // Send 100 messages
                            for (int ii = 0; ii < 20000; ++ii)
                            {
                                anOutputChannel.SendMessage(ii.ToString());
                            }
                        });
                    aThreads.Add(aThread);
                }

                anInputChannel.StartListening();

                aThreads.ForEach(x => x.Start());
                aThreads.ForEach(x => x.Join());

                Assert.IsTrue(anAllMessagesHandledSignal.WaitOne(100), "Processing of messages timeout.");
            }
            finally
            {
                anInputChannel.StopListening();
            }

            // Check
            Assert.AreEqual(20000 * 4, aReceivedMessages.Count);
        }

        [Test]
        public virtual void B02_Create100Listeners()
        {
            List<IInputChannel> aInputChannels = new List<IInputChannel>();

            try
            {
                for (int i = 0; i < 100; ++i)
                {
                    IInputChannel anInputChannel = MessagingSystemFactory.CreateInputChannel("net.pipe://127.0.0.1/" + i.ToString());
                    aInputChannels.Add(anInputChannel);

                    // This will create many threads
                    anInputChannel.StartListening();
                }
            }
            catch (Exception)
            {
                foreach (IInputChannel anInputChannel in aInputChannels)
                {
                    anInputChannel.StopListening();
                }

                throw;
            }


            // Event signalint the end of the message handling
            AutoResetEvent anAllMessagesHandledSignal = new AutoResetEvent(false);

            // observer channel 10
            string aReceivedMessage = "";
            aInputChannels[10].MessageReceived += (x, y) =>
            {
                aReceivedMessage = (string) y.Message;

                anAllMessagesHandledSignal.Set();
            };

            // Try to send something
            IOutputChannel anOutputChannel = MessagingSystemFactory.CreateOutputChannel("net.pipe://127.0.0.1/10");

            try
            {
                anOutputChannel.SendMessage("My message.");

                Assert.IsTrue(anAllMessagesHandledSignal.WaitOne(200), "Time out - the message was not received.");
            }
            finally
            {
                foreach (IInputChannel anInputChannel in aInputChannels)
                {
                    anInputChannel.StopListening();
                }
            }

            Assert.AreEqual("My message.", aReceivedMessage);
        }
    }
}


#endif