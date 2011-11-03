using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Threading;
using Eneter.Messaging.MessagingSystems.Composites.ReliableMessagingComposit;
using Eneter.Messaging.EndPoints.TypedMessages;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;

namespace Eneter.MessagingUnitTests.EndPoints.TypedReliableMessages
{
    public abstract class TypedReliableMessagesBaseTester
    {
        protected void Setup(IReliableMessagingFactory messagingSystemFactory, string channelId, ISerializer serializer)
        {
            MessagingSystemFactory = messagingSystemFactory;

            ReliableDuplexOutputChannel = MessagingSystemFactory.CreateDuplexOutputChannel(channelId);
            ReliableDuplexInputChannel = MessagingSystemFactory.CreateDuplexInputChannel(channelId);

            IReliableTypedMessagesFactory aMessageFactory = new ReliableTypedMessagesFactory(serializer);
            MessageSender = aMessageFactory.CreateReliableDuplexTypedMessageSender<int, int>();
            MessageReceiver = aMessageFactory.CreateReliableDuplexTypedMessageReceiver<int, int>();
        }

        [Test]
        public void SendReceive_1Message()
        {
            AutoResetEvent aMessagesProcessedEvent = new AutoResetEvent(false);

            string aSentResponseMessageId = "aaa";
            int aReceivedMessage = 0;
            MessageReceiver.MessageReceived += (x, y) =>
                {
                    aReceivedMessage = y.RequestMessage;

                    // Send the response
                    aSentResponseMessageId = MessageReceiver.SendResponseMessage(y.ResponseReceiverId, 1000);
                };

            string aDeliveredResponseMessage = "bbb";
            MessageReceiver.ResponseMessageDelivered += (x, y) =>
                {
                    aDeliveredResponseMessage = y.MessageId;
                    aMessagesProcessedEvent.Set();
                };
            

            int aReceivedResponse = 0;
            MessageSender.ResponseReceived += (x, y) =>
                {
                    aReceivedResponse = y.ResponseMessage;
                };

            string aDeliveredMessage = "ccc";
            MessageSender.MessageDelivered += (x, y) =>
                {
                    aDeliveredMessage = y.MessageId;
                };


            string aSentMessageId = "ddd";
            try
            {
                MessageReceiver.AttachReliableInputChannel(ReliableDuplexInputChannel);
                MessageSender.AttachReliableOutputChannel(ReliableDuplexOutputChannel);

                aSentMessageId = MessageSender.SendRequestMessage(2000);

                // Wait for the signal that the message is received.
                Assert.IsTrue(aMessagesProcessedEvent.WaitOne(200));
            }
            finally
            {
                MessageSender.DetachReliableOutputChannel();
                MessageReceiver.DetachReliableInputChannel();
            }

            // Check received values
            Assert.AreEqual(2000, aReceivedMessage);
            Assert.AreEqual(1000, aReceivedResponse);

            Assert.AreEqual(aSentMessageId, aDeliveredMessage);
            Assert.AreEqual(aSentResponseMessageId, aDeliveredResponseMessage);
        }

        [Test]
        public void SendReceive_MultiThreadAccess_1000Messages()
        {
            AutoResetEvent aMessagesProcessedEvent = new AutoResetEvent(false);

            List<string> aSentResponseMessageIds = new List<string>();
            List<int> aReceivedMessages = new List<int>();
            MessageReceiver.MessageReceived += (x, y) =>
                {
                    lock (aReceivedMessages)
                    {
                        aReceivedMessages.Add(y.RequestMessage);

                        // Send the response
                        string aSentResponseMessageId = MessageReceiver.SendResponseMessage(y.ResponseReceiverId, y.RequestMessage + 1000);
                        aSentResponseMessageIds.Add(aSentResponseMessageId);
                    }
                };

            List<string> aDeliveredResponseMessages = new List<string>();
            MessageReceiver.ResponseMessageDelivered += (x, y) =>
                {
                    lock (aDeliveredResponseMessages)
                    {
                        string aDeliveredResponseMessageId = y.MessageId;
                        aDeliveredResponseMessages.Add(aDeliveredResponseMessageId);

                        if (aDeliveredResponseMessages.Count == 1000)
                        {
                            aMessagesProcessedEvent.Set();
                        }
                    }
                };


            List<int> aReceivedResponses = new List<int>();
            MessageSender.ResponseReceived += (x, y) =>
                {
                    lock (aReceivedResponses)
                    {
                        aReceivedResponses.Add(y.ResponseMessage);
                    }
                };

            List<string> aDeliveredMessages = new List<string>();
            MessageSender.MessageDelivered += (x, y) =>
                {
                    lock (aDeliveredMessages)
                    {
                        aDeliveredMessages.Add(y.MessageId);
                    }
                };


            List<string> aSentMessageIds = new List<string>();
            try
            {
                MessageReceiver.AttachReliableInputChannel(ReliableDuplexInputChannel);
                MessageSender.AttachReliableOutputChannel(ReliableDuplexOutputChannel);

                for (int i = 0; i < 1000; ++i)
                {
                    string aSentMessageId = MessageSender.SendRequestMessage(i);
                    aSentMessageIds.Add(aSentMessageId);
                }

                // Wait for the signal that the message is received.
                Assert.IsTrue(aMessagesProcessedEvent.WaitOne(50000));
            }
            finally
            {
                MessageSender.DetachReliableOutputChannel();
                MessageReceiver.DetachReliableInputChannel();
            }

            // Check received values
            Assert.AreEqual(1000, aReceivedMessages.Count);
            aReceivedMessages.Sort();
            for (int i = 0; i < 1000; ++i)
            {
                Assert.AreEqual(i, aReceivedMessages[i]);
            }

            Assert.AreEqual(1000, aReceivedResponses.Count);
            aReceivedResponses.Sort();
            for (int i = 0; i < 1000; ++i)
            {
                Assert.AreEqual(i + 1000, aReceivedResponses[i]);
            }

            Assert.AreEqual(1000, aDeliveredMessages.Count);
            aDeliveredMessages.Sort();
            aSentMessageIds.Sort();
            aDeliveredMessages.SequenceEqual(aSentMessageIds);

            Assert.AreEqual(1000, aDeliveredResponseMessages.Count);
            aDeliveredResponseMessages.Sort();
            aSentResponseMessageIds.Sort();
            aDeliveredResponseMessages.SequenceEqual(aSentResponseMessageIds);
        }



        protected IReliableMessagingFactory MessagingSystemFactory { get; set; }
        protected IReliableDuplexOutputChannel ReliableDuplexOutputChannel { get; set; }
        protected IReliableDuplexInputChannel ReliableDuplexInputChannel { get; set; }

        protected IReliableTypedMessageSender<int, int> MessageSender { get; set; }
        protected IReliableTypedMessageReceiver<int, int> MessageReceiver { get; set; }
    }
}
