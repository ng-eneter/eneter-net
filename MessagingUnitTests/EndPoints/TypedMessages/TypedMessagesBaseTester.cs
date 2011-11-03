using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.EndPoints.TypedMessages;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using System.Threading;
using Eneter.Messaging.DataProcessing.Serializing;

namespace Eneter.MessagingUnitTests.EndPoints.TypedMessages 
{
    public abstract class TypedMessagesBaseTester
    {
        protected void Setup(IMessagingSystemFactory messagingSystemFactory, string channelId, ISerializer serializer)
        {
            MessagingSystemFactory = messagingSystemFactory;

            OutputChannel = MessagingSystemFactory.CreateOutputChannel(channelId);
            InputChannel = MessagingSystemFactory.CreateInputChannel(channelId);

            ITypedMessagesFactory aMessageFactory = new TypedMessagesFactory(serializer);
            MessageSender = aMessageFactory.CreateTypedMessageSender<Fake_TypedMessage>();
            MessageReceiver = aMessageFactory.CreateTypedMessageReceiver<Fake_TypedMessage>();
        }

        [Test]
        public void SendReceive_1Message()
        {
            MessageSender.AttachOutputChannel(OutputChannel);

            // The test can be performed from more thread therefore we must synchronize.
            ManualResetEvent aMessageReceivedEvent = new ManualResetEvent(false);

            Fake_TypedMessage aReceivedMessage = null;
            MessageReceiver.MessageReceived += (x, y) =>
            {
                aReceivedMessage = y.MessageData;

                // Signal that the message was received.
                aMessageReceivedEvent.Set();
            };
            MessageReceiver.AttachInputChannel(InputChannel);

            try
            {
                Fake_TypedMessage aMessage = new Fake_TypedMessage();
                aMessage.FirstName = "Janko";
                aMessage.SecondName = "Mrkvicka";
                
                MessageSender.SendMessage(aMessage);

                // Wait for the signal that the message is received.
                Assert.IsTrue(aMessageReceivedEvent.WaitOne(200));
            }
            finally
            {
                MessageReceiver.DetachInputChannel();
            }

            // Check received values
            Assert.AreEqual("Janko", aReceivedMessage.FirstName);
            Assert.AreEqual("Mrkvicka", aReceivedMessage.SecondName);
        }

        [Test]
        public void SendReceive_MultiThreadAccess_1000Messages()
        {
            MessageSender.AttachOutputChannel(OutputChannel);

            // The test can be performed from more thread therefore we must synchronize.
            ManualResetEvent aMessageReceivedEvent = new ManualResetEvent(false);

            List<Fake_TypedMessage> aReceivedMessages = new List<Fake_TypedMessage>();
            MessageReceiver.MessageReceived += (x, y) =>
            {
                lock (aReceivedMessages)
                {
                    aReceivedMessages.Add(y.MessageData);
                }

                if (aReceivedMessages.Count == 1000)
                {
                    // Signal that the message was received.
                    aMessageReceivedEvent.Set();
                }
            };
            MessageReceiver.AttachInputChannel(InputChannel);

            Fake_TypedMessage aMessage = new Fake_TypedMessage();
            aMessage.FirstName = "Janko";
            aMessage.SecondName = "Mrkvicka";

            try
            {
                List<Thread> aThreads = new List<Thread>();

                for (int i = 0; i < 10; ++i)
                {
                    Thread aThread = new Thread(() =>
                    {
                        for (int ii = 0; ii < 100; ++ii)
                        {
                            MessageSender.SendMessage(aMessage);
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
            aReceivedMessages.ForEach(x => Assert.AreEqual(aMessage.FirstName, x.FirstName));
            aReceivedMessages.ForEach(x => Assert.AreEqual(aMessage.SecondName, x.SecondName));
        }


        protected IMessagingSystemFactory MessagingSystemFactory { get; set; }
        protected IOutputChannel OutputChannel { get; set; }
        protected IInputChannel InputChannel { get; set; }

        protected ITypedMessageSender<Fake_TypedMessage> MessageSender { get; set; }
        protected ITypedMessageReceiver<Fake_TypedMessage> MessageReceiver { get; set; }
    }
}
