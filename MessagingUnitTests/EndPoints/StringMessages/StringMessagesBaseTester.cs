using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using NUnit.Framework;
using Eneter.Messaging.EndPoints.StringMessages;
using System.Threading;

namespace Eneter.MessagingUnitTests.EndPoints.StringMessages
{
    public abstract class StringMessagesBaseTester
    {

        protected void Setup(IMessagingSystemFactory messagingSystemFactory, string channelId)
        {
            MessagingSystemFactory = messagingSystemFactory;

            OutputChannel = MessagingSystemFactory.CreateOutputChannel(channelId);
            InputChannel = MessagingSystemFactory.CreateInputChannel(channelId);

            IStringMessagesFactory aMessageFactory = new StringMessagesFactory();
            MessageSender = aMessageFactory.CreateStringMessageSender();
            MessageReceiver = aMessageFactory.CreateStringMessageReceiver();
        }

        [Test]
        public void SendReceive_1Message()
        {
            MessageSender.AttachOutputChannel(OutputChannel);

            // The test can be performed from more thread therefore we must synchronize.
            ManualResetEvent aMessageReceivedEvent = new ManualResetEvent(false);

            string aReceivedMessage = "";
            MessageReceiver.MessageReceived += (x, y) =>
                {
                    aReceivedMessage = y.Message;

                    // Signal that the message was received.
                    aMessageReceivedEvent.Set();
                };
            MessageReceiver.AttachInputChannel(InputChannel);

            try
            {
                MessageSender.SendMessage("Message");

                // Wait for the signal that the message is received.
                Assert.IsTrue(aMessageReceivedEvent.WaitOne(200));
            }
            finally
            {
                MessageReceiver.DetachInputChannel();
            }

            // Check received values
            Assert.AreEqual("Message", aReceivedMessage);
        }

        [Test]
        public void SendReceive_MultiThreadAccess_1000Messages()
        {
            MessageSender.AttachOutputChannel(OutputChannel);

            // The test can be performed from more thread therefore we must synchronize.
            ManualResetEvent aMessageReceivedEvent = new ManualResetEvent(false);

            List<string> aReceivedMessages = new List<string>();
            MessageReceiver.MessageReceived += (x, y) =>
            {
                lock (aReceivedMessages)
                {
                    aReceivedMessages.Add(y.Message);
                }

                if (aReceivedMessages.Count == 1000)
                {
                    // Signal that the message was received.
                    aMessageReceivedEvent.Set();
                }
            };
            MessageReceiver.AttachInputChannel(InputChannel);

            try
            {
                List<Thread> aThreads = new List<Thread>();

                for (int i = 0; i < 10; ++i)
                {
                    Thread aThread = new Thread(() =>
                        {
                            for (int ii = 0; ii < 100; ++ii)
                            {
                                MessageSender.SendMessage("Message");
                                Thread.Sleep(1);
                            }
                        });

                    aThreads.Add(aThread);
                }

                aThreads.ForEach(x => x.Start());

                // Wait for the signal that the message is received.
                Assert.IsTrue(aMessageReceivedEvent.WaitOne(10000));
            }
            finally
            {
                MessageReceiver.DetachInputChannel();
            }

            // Check received values
            Assert.AreEqual(1000, aReceivedMessages.Count);
            aReceivedMessages.ForEach(x => Assert.AreEqual("Message", x));
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void AttachOutputChannelAgain()
        {
            try
            {
                MessageSender.AttachOutputChannel(OutputChannel);

                // The second attach should throw the exception.
                IOutputChannel anOutputChannel = MessagingSystemFactory.CreateOutputChannel(OutputChannel.ChannelId);
                MessageSender.AttachOutputChannel(anOutputChannel);
            }
            finally
            {
                MessageSender.DetachOutputChannel();
            }
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void AttachInputChannelAgain()
        {
            try
            {
                MessageReceiver.AttachInputChannel(InputChannel);

                // The second attach should throw the exception.
                IInputChannel anInputChannel = MessagingSystemFactory.CreateInputChannel(InputChannel.ChannelId);
                MessageReceiver.AttachInputChannel(anInputChannel);
            }
            finally
            {
                MessageReceiver.DetachInputChannel();
            }
        }

        protected IMessagingSystemFactory MessagingSystemFactory { get; set; }
        protected IOutputChannel OutputChannel { get; set; }
        protected IInputChannel InputChannel { get; set; }

        protected IStringMessageSender MessageSender { get; set; }
        protected IStringMessageReceiver MessageReceiver { get; set; }
    }
}
