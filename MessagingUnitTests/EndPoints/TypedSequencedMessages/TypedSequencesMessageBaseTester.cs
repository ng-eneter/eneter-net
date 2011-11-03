using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.EndPoints.TypedSequencedMessages;
using NUnit.Framework;
using System.Threading;
using Eneter.Messaging.Diagnostic;

namespace Eneter.MessagingUnitTests.EndPoints.TypedSequencedMessages
{
    public abstract class TypedSequencesMessageBaseTester
    {
        protected void Setup(IMessagingSystemFactory messagingSystemFactory, string channelId, ISerializer serializer)
        {
            MessagingSystemFactory = messagingSystemFactory;

            OutputChannel = MessagingSystemFactory.CreateOutputChannel(channelId);
            InputChannel = MessagingSystemFactory.CreateInputChannel(channelId);

            ITypedSequencedMessagesFactory aMessageFactory = new TypedSequencedMessagesFactory(serializer);
            MessageSender = aMessageFactory.CreateTypedSequencedMessageSender<Fake_Data>();
            MessageReceiver = aMessageFactory.CreateTypedSequencedMessageReceiver<Fake_Data>();
        }

        [Test]
        public void SendReceive_1Sequence()
        {
            MessageSender.AttachOutputChannel(OutputChannel);

            // The test can be performed from more thread therefore we must synchronize.
            ManualResetEvent aMessageReceivedEvent = new ManualResetEvent(false);

            List<TypedSequencedMessageReceivedEventArgs<Fake_Data>> aReceivedMessages = new List<TypedSequencedMessageReceivedEventArgs<Fake_Data>>();
            MessageReceiver.MessageReceived += (x, y) =>
                {
                    lock (aReceivedMessages)
                    {
                        aReceivedMessages.Add(y);
                    }

                    if (aReceivedMessages.Count == 100)
                    {
                        // Signal that the sequence is received.
                        aMessageReceivedEvent.Set();
                    }
                };
            MessageReceiver.AttachInputChannel(InputChannel);

            try
            {
                // Send the sequence of 100 messages.
                for (int i = 0; i < 100; ++i)
                {
                    Fake_Data aMessage = new Fake_Data();
                    aMessage.Number = i;

                    MessageSender.SendMessage(aMessage, "MySequenceId", i == 99);
                }

                // Wait for the signal that the sequence is received.
                Assert.IsTrue(aMessageReceivedEvent.WaitOne(200));
            }
            finally
            {
                MessageReceiver.DetachInputChannel();
            }

            // Check received values
            Assert.AreEqual(100, aReceivedMessages.Count);

            for (int ii = 0; ii < 100; ++ii)
            {
                Assert.AreEqual(ii, aReceivedMessages[ii].MessageData.Number);
                Assert.AreEqual("MySequenceId", aReceivedMessages[ii].SequenceId);
                Assert.AreEqual(null, aReceivedMessages[ii].ReceivingError);
                Assert.AreEqual(ii == 99, aReceivedMessages[ii].IsSequenceCompleted);
            }
        }

        [Test]
        public void SendReceive_MoreSequencesInParallel()
        {
            MessageSender.AttachOutputChannel(OutputChannel);

            AutoResetEvent aMessageReceivedEvent = new AutoResetEvent(false);

            int aNumberOfCompleted = 0;
            List<TypedSequencedMessageReceivedEventArgs<Fake_Data>> aReceivedMessages = new List<TypedSequencedMessageReceivedEventArgs<Fake_Data>>();
            MessageReceiver.MessageReceived += (x, y) =>
                {
                    aReceivedMessages.Add(y);

                    // Signal that the response message was received -> the loop is closed.
                    if (y.IsSequenceCompleted)
                    {
                        ++aNumberOfCompleted;

                        // We have 10 threads pruducing the sequence.
                        if (aNumberOfCompleted == 10)
                        {
                            aMessageReceivedEvent.Set();
                        }
                    }
                };
            MessageReceiver.AttachInputChannel(InputChannel);

            try
            {
                List<Thread> aThreads = new List<Thread>();

                // 10 threads producing in parallel one sequence each. Every sequence has 100 fragments.
                for (int i = 0; i < 10; ++i)
                {
                    Thread aThread = new Thread(x =>
                        {
                            string aSequenceId = (string)x;
                            int k = int.Parse(aSequenceId);

                            for (int ii = 0; ii < 100; ++ii)
                            {
                                Fake_Data aFakeMsg = new Fake_Data(100 * k + ii);
                                MessageSender.SendMessage(aFakeMsg, aSequenceId, ii == 99);
                                Thread.Sleep(1);
                            }
                        });

                    aThreads.Add(aThread);
                }

                for (int iii = 0; iii < 10; ++iii)
                {
                    aThreads[iii].Start(iii.ToString());
                }

                // Wait for the signal that the message is received.
                aMessageReceivedEvent.WaitOne();
                //Assert.IsTrue(aMessageReceivedEvent.WaitOne(3000), "Wait for the whole test - timeout.");
            }
            finally
            {
                MessageReceiver.DetachInputChannel();
            }

            // Check received values
            Assert.AreEqual(10 * 100, aReceivedMessages.Count);
            // Check that 10 sequences with 100 fragments were received as requests.
            for (int i = 0; i < 10; ++i)
            {
                Assert.AreEqual(100, aReceivedMessages.Count(x => x.SequenceId == i.ToString()));
            }
            // Check that each sequence has just one completed flag.
            for (int i = 0; i < 10; ++i)
            {
                Assert.AreEqual(1, aReceivedMessages.Count(x => x.SequenceId == i.ToString() && x.IsSequenceCompleted));
            }
            // Check that values (messages) in each sequence are correct.
            for (int i = 0; i < 10; ++i)
            {
                // Note: The method List.FindAll(Predicate x) returning the list is not avsailable in Silverlight.
                //       Therefore we must use a little workaround.
                IEnumerable<TypedSequencedMessageReceivedEventArgs<Fake_Data>> aSequencedMsgs = aReceivedMessages.Where(x => x.SequenceId == i.ToString());
                List<TypedSequencedMessageReceivedEventArgs<Fake_Data>> aSequencedMessages = aSequencedMsgs.ToList();

                Assert.AreEqual(100, aSequencedMessages.Count());
                for (int ii = 0; ii < 100; ++ii)
                {
                    Assert.AreEqual(100 * i + ii, aSequencedMessages[ii].MessageData.Number);
                }
            }
        }

        protected IMessagingSystemFactory MessagingSystemFactory { get; set; }
        protected IOutputChannel OutputChannel { get; set; }
        protected IInputChannel InputChannel { get; set; }

        protected ITypedSequencedMessageSender<Fake_Data> MessageSender { get; set; }
        protected ITypedSequencedMessageReceiver<Fake_Data> MessageReceiver { get; set; }
    }
}
