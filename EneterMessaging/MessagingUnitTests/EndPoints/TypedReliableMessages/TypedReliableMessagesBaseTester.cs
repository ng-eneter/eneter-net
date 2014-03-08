using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Threading;
using Eneter.Messaging.EndPoints.TypedMessages;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.MessagingUnitTests.EndPoints.TypedReliableMessages
{
    public abstract class TypedReliableMessagesBaseTester
    {
        protected void Setup(IMessagingSystemFactory messagingSystemFactory, string channelId, ISerializer serializer)
        {
            MessagingSystemFactory = messagingSystemFactory;

            DuplexOutputChannel = MessagingSystemFactory.CreateDuplexOutputChannel(channelId);
            DuplexInputChannel = MessagingSystemFactory.CreateDuplexInputChannel(channelId);

            IReliableTypedMessagesFactory aMessageFactory = new ReliableTypedMessagesFactory(TimeSpan.FromMilliseconds(12000), serializer);
            MessageSender = aMessageFactory.CreateReliableDuplexTypedMessageSender<int, int>();
            MessageReceiver = aMessageFactory.CreateReliableDuplexTypedMessageReceiver<int, int>();

            mySerializer = serializer;
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

            string aDeliveredMessageId = "ccc";
            MessageSender.MessageDelivered += (x, y) =>
                {
                    aDeliveredMessageId = y.MessageId;
                };


            string aSentMessageId = "ddd";
            try
            {
                MessageReceiver.AttachDuplexInputChannel(DuplexInputChannel);
                MessageSender.AttachDuplexOutputChannel(DuplexOutputChannel);

                aSentMessageId = MessageSender.SendRequestMessage(2000);

                // Wait for the signal that the message is received.
                Assert.IsTrue(aMessagesProcessedEvent.WaitOne(200));
            }
            finally
            {
                MessageSender.DetachDuplexOutputChannel();
                MessageReceiver.DetachDuplexInputChannel();
            }

            // Check received values
            Assert.AreEqual(2000, aReceivedMessage);
            Assert.AreEqual(1000, aReceivedResponse);

            Assert.AreEqual(aSentMessageId, aDeliveredMessageId);
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

            List<string> aDeliveredResponseMessageIds = new List<string>();
            MessageReceiver.ResponseMessageDelivered += (x, y) =>
                {
                    lock (aDeliveredResponseMessageIds)
                    {
                        string aDeliveredResponseMessageId = y.MessageId;
                        aDeliveredResponseMessageIds.Add(aDeliveredResponseMessageId);

                        if (aDeliveredResponseMessageIds.Count == 1000)
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

            List<string> aDeliveredMessageIds = new List<string>();
            MessageSender.MessageDelivered += (x, y) =>
                {
                    lock (aDeliveredMessageIds)
                    {
                        aDeliveredMessageIds.Add(y.MessageId);
                    }
                };


            List<string> aSentMessageIds = new List<string>();
            try
            {
                MessageReceiver.AttachDuplexInputChannel(DuplexInputChannel);
                MessageSender.AttachDuplexOutputChannel(DuplexOutputChannel);

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
                MessageSender.DetachDuplexOutputChannel();
                MessageReceiver.DetachDuplexInputChannel();
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

            Assert.AreEqual(1000, aDeliveredMessageIds.Count);
            aDeliveredMessageIds.Sort();
            aSentMessageIds.Sort();
            Assert.IsTrue(aDeliveredMessageIds.SequenceEqual(aSentMessageIds));

            Assert.AreEqual(1000, aDeliveredResponseMessageIds.Count);
            aDeliveredResponseMessageIds.Sort();
            aSentResponseMessageIds.Sort();
            Assert.IsTrue(aDeliveredResponseMessageIds.SequenceEqual(aSentResponseMessageIds));
        }


        [Test]
        public void NotAcknowledgedRequest()
        {
            // To simulate not delivering the acknowledge for the request, the receiver is not reliable.
            IDuplexTypedMessageReceiver<int, int> aReceiver = new DuplexTypedMessagesFactory(mySerializer).CreateDuplexTypedMessageReceiver<int, int>();
            IReliableTypedMessageSender<int, int> aSender = new ReliableTypedMessagesFactory(TimeSpan.FromMilliseconds(500), mySerializer).CreateReliableDuplexTypedMessageSender<int, int>();

            AutoResetEvent aMessagesProcessedEvent = new AutoResetEvent(false);

            string anAcknowledId = "";
            aSender.MessageDelivered += (x, y) =>
                {
                    anAcknowledId = y.MessageId;

                    aMessagesProcessedEvent.Set();
                };

            string aNotAcknowledgedId = "";
            aSender.MessageNotDelivered += (x, y) =>
                {
                    aNotAcknowledgedId = y.MessageId;
                    
                    aMessagesProcessedEvent.Set();
                };

            string aSentMessageId;
            try
            {
                aReceiver.AttachDuplexInputChannel(DuplexInputChannel);
                aSender.AttachDuplexOutputChannel(DuplexOutputChannel);

                aSentMessageId = aSender.SendRequestMessage(2000);

                // Wait for the signal that the message is received.
                Assert.IsTrue(aMessagesProcessedEvent.WaitOne(2000));
            }
            finally
            {
                aSender.DetachDuplexOutputChannel();
                aReceiver.DetachDuplexInputChannel();
            }

            // Check received values
            Assert.AreEqual("", anAcknowledId);
            Assert.AreEqual(aSentMessageId, aNotAcknowledgedId);
        }

        [Test]
        public void NotAcknowledgedResponse()
        {
            // To simulate not delivering the acknowledge for the response, the sender is not reliable.
            IReliableTypedMessageReceiver<int, int> aReceiver = new ReliableTypedMessagesFactory(TimeSpan.FromMilliseconds(500), mySerializer).CreateReliableDuplexTypedMessageReceiver<int, int>();
            IDuplexTypedMessageSender<int, int> aSender = new DuplexTypedMessagesFactory(mySerializer).CreateDuplexTypedMessageSender<int, int>();

            AutoResetEvent aMessagesProcessedEvent = new AutoResetEvent(false);

            string anAcknowledId = "";
            aReceiver.ResponseMessageDelivered += (x, y) =>
            {
                anAcknowledId = y.MessageId;

                aMessagesProcessedEvent.Set();
            };

            string aNotAcknowledgedId = "";
            aReceiver.ResponseMessageNotDelivered += (x, y) =>
            {
                aNotAcknowledgedId = y.MessageId;

                aMessagesProcessedEvent.Set();
            };

            string aSentMessageId;
            try
            {
                aReceiver.AttachDuplexInputChannel(DuplexInputChannel);
                aSender.AttachDuplexOutputChannel(DuplexOutputChannel);

                // Send the response message.
                aSentMessageId = aReceiver.SendResponseMessage(aSender.AttachedDuplexOutputChannel.ResponseReceiverId, 2000);

                // Wait for the signal that the message is received.
                Assert.IsTrue(aMessagesProcessedEvent.WaitOne(2000));
            }
            finally
            {
                aSender.DetachDuplexOutputChannel();
                aReceiver.DetachDuplexInputChannel();
            }

            // Check received values
            Assert.AreEqual("", anAcknowledId);
            Assert.AreEqual(aSentMessageId, aNotAcknowledgedId);
        }



        protected IMessagingSystemFactory MessagingSystemFactory { get; set; }
        protected IDuplexOutputChannel DuplexOutputChannel { get; set; }
        protected IDuplexInputChannel DuplexInputChannel { get; set; }

        protected ISerializer mySerializer;
        protected IReliableTypedMessageSender<int, int> MessageSender { get; set; }
        protected IReliableTypedMessageReceiver<int, int> MessageReceiver { get; set; }
    }
}
