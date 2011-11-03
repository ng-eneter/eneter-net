using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.EndPoints.TypedSequencedMessages;
using Eneter.Messaging.DataProcessing.Serializing;
using NUnit.Framework;
using System.Threading;
using Eneter.Messaging.MessagingSystems.Composites.ReliableMessagingComposit;

namespace Eneter.MessagingUnitTests.EndPoints.TypedSequencesReliableRequestResponse
{
    public abstract class TypedSequencedReliableBaseTester
    {
        protected void Setup(IReliableMessagingFactory messagingSystemFactory, string channelId, ISerializer serializer)
        {
            MessagingSystemFactory = messagingSystemFactory;

            DuplexOutputChannel = MessagingSystemFactory.CreateDuplexOutputChannel(channelId);
            DuplexInputChannel = MessagingSystemFactory.CreateDuplexInputChannel(channelId);

            IReliableTypedSequencedMessagesFactory aMessageFactory = new ReliableTypedSequencedMessagesFactory(serializer);
            MessageSender = aMessageFactory.CreateReliableTypedSequencedMessageSender<int, int>();
            MessageReceiver = aMessageFactory.CreateReliableTypedSequencedMessageReceiver<int, int>();
        }


        [Test]
        public void SendReceive_1Sequence()
        {
            AutoResetEvent aMessagesProcessedEvent = new AutoResetEvent(false);


            List<string> aSentResponseMessageIds = new List<string>();
            List<TypedSequencedRequestReceivedEventArgs<int>> aReceivedRequests = new List<TypedSequencedRequestReceivedEventArgs<int>>();
            MessageReceiver.MessageReceived += (x, y) =>
                {
                    lock (aReceivedRequests)
                    {
                        aReceivedRequests.Add(y);
                    }

                    // Send the response
                    aSentResponseMessageIds.Add(MessageReceiver.SendResponseMessage(y.ResponseReceiverId, y.RequestMessage - 1000, "ResponseSequence1", y.IsSequenceCompleted));
                };

            List<string> aDeliveredResponseMessageIds = new List<string>();
            MessageReceiver.ResponseMessageDelivered += (x, y) =>
                {
                    aDeliveredResponseMessageIds.Add(y.MessageId);

                    if (aDeliveredResponseMessageIds.Count == 3)
                    {
                        aMessagesProcessedEvent.Set();
                    }
                };


            List<TypedSequencedResponseReceivedEventArgs<int>> aReceivedResponses = new List<TypedSequencedResponseReceivedEventArgs<int>>();
            MessageSender.ResponseReceived += (x, y) =>
                {
                    lock (aReceivedResponses)
                    {
                        aReceivedResponses.Add(y);
                    }
                };

            List<string> aDeliveredMessageIds = new List<string>();
            MessageSender.MessageDelivered += (x, y) =>
                {
                    aDeliveredMessageIds.Add(y.MessageId);
                };

            List<string> aSentMessageIds = new List<string>();

            try
            {
                MessageReceiver.AttachReliableInputChannel(DuplexInputChannel);
                MessageSender.AttachReliableOutputChannel(DuplexOutputChannel);

                aSentMessageIds.Add(MessageSender.SendMessage(2000, "RequestSequence1", false));
                aSentMessageIds.Add(MessageSender.SendMessage(2001, "RequestSequence1", false));
                aSentMessageIds.Add(MessageSender.SendMessage(2002, "RequestSequence1", true));

                // Wait for the signal that the message is received.
                //Assert.IsTrue(aMessageReceivedEvent.WaitOne(2000));
                Assert.IsTrue(aMessagesProcessedEvent.WaitOne());
            }
            finally
            {
                MessageSender.DetachReliableOutputChannel();
                MessageReceiver.DetachReliableInputChannel();
            }

            // Check received values
            Assert.AreEqual(3, aReceivedRequests.Count);
            aReceivedRequests.ForEach(x => Assert.AreEqual("RequestSequence1", x.SequenceId));
            Assert.AreEqual(2000, aReceivedRequests[0].RequestMessage);
            Assert.AreEqual(2001, aReceivedRequests[1].RequestMessage);
            Assert.AreEqual(2002, aReceivedRequests[2].RequestMessage);
            Assert.IsFalse(aReceivedRequests[0].IsSequenceCompleted);
            Assert.IsFalse(aReceivedRequests[1].IsSequenceCompleted);
            Assert.IsTrue(aReceivedRequests[2].IsSequenceCompleted);

            Assert.AreEqual(3, aReceivedResponses.Count);
            aReceivedResponses.ForEach(x => Assert.AreEqual("ResponseSequence1", x.SequenceId));
            Assert.AreEqual(1000, aReceivedResponses[0].ResponseMessage);
            Assert.AreEqual(1001, aReceivedResponses[1].ResponseMessage);
            Assert.AreEqual(1002, aReceivedResponses[2].ResponseMessage);
            Assert.IsFalse(aReceivedResponses[0].IsSequenceCompleted);
            Assert.IsFalse(aReceivedResponses[1].IsSequenceCompleted);
            Assert.IsTrue(aReceivedResponses[2].IsSequenceCompleted);

            Assert.AreEqual(3, aDeliveredMessageIds.Count);
            Assert.AreEqual(3, aDeliveredResponseMessageIds.Count);

            aDeliveredMessageIds.Sort();
            aSentMessageIds.Sort();
            Assert.IsTrue(aSentMessageIds.SequenceEqual(aDeliveredMessageIds));

            aDeliveredResponseMessageIds.Sort();
            aSentResponseMessageIds.Sort();
            Assert.IsTrue(aSentResponseMessageIds.SequenceEqual(aDeliveredResponseMessageIds));
        }




        protected IReliableMessagingFactory MessagingSystemFactory { get; set; }
        protected IReliableDuplexOutputChannel DuplexOutputChannel { get; set; }
        protected IReliableDuplexInputChannel DuplexInputChannel { get; set; }


        protected IReliableTypedSequencedMessageSender<int, int> MessageSender { get; set; }
        protected IReliableTypedSequencedMessageReceiver<int, int> MessageReceiver { get; set; }
    }
}
