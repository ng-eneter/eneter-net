using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using NUnit.Framework;
using System.Threading;
using Eneter.Messaging.EndPoints.StringMessages;
using Eneter.Messaging.MessagingSystems.Composites.ReliableMessagingComposit;

namespace Eneter.MessagingUnitTests.EndPoints.StringReliableMessages
{
    public abstract class StringReliableMessagesBaseTester
    {
        protected void Setup(IReliableMessagingFactory messagingSystemFactory, string channelId)
        {
            MessagingSystemFactory = messagingSystemFactory;

            ReliableDuplexOutputChannel = MessagingSystemFactory.CreateDuplexOutputChannel(channelId);
            ReliableDuplexInputChannel = MessagingSystemFactory.CreateDuplexInputChannel(channelId);

            IReliableStringMessagesFactory aMessageFactory = new ReliableStringMessagesFactory();
            MessageSender = aMessageFactory.CreateReliableDuplexStringMessageSender();
            MessageReceiver = aMessageFactory.CreateReliableDuplexStringMessageReceiver();
        }


        [Test]
        public void SendReceive_1Message()
        {
            string aReceivedMessage = "";
            MessageReceiver.RequestReceived += (x, y) =>
                {
                    aReceivedMessage = y.RequestMessage;

                    // Send the response
                    MessageReceiver.SendResponseMessage(y.ResponseReceiverId, "Response");
                };

            AutoResetEvent aResponseMessageConfirmedEvent = new AutoResetEvent(false);
            string aConfirmedResponseMessage;
            MessageReceiver.ResponseMessageDelivered += (x, y) =>
                {
                    aConfirmedResponseMessage = y.MessageId;
                    aResponseMessageConfirmedEvent.Set();
                };


            string aConfirmedMessage;
            MessageSender.MessageDelivered += (x, y) =>
                {
                    aConfirmedMessage = y.MessageId;
                };

            string aReceivedResponse = "";
            MessageSender.ResponseReceived += (x, y) =>
                {
                    aReceivedResponse = y.ResponseMessage;
                };
            

            try
            {
                MessageReceiver.AttachReliableInputChannel(ReliableDuplexInputChannel);
                MessageSender.AttachReliableOutputChannel(ReliableDuplexOutputChannel);

                MessageSender.SendMessage("Message");

                // Wait for the signal that messages are processed.
                Assert.IsTrue(aResponseMessageConfirmedEvent.WaitOne(200));
            }
            finally
            {
                MessageSender.DetachReliableOutputChannel();
                MessageReceiver.DetachReliableInputChannel();
            }

            // Check received values
            Assert.AreEqual("Message", aReceivedMessage);
            Assert.AreEqual("Response", aReceivedResponse);
        }


        protected IReliableMessagingFactory MessagingSystemFactory { get; set; }
        protected IReliableDuplexOutputChannel ReliableDuplexOutputChannel { get; set; }
        protected IReliableDuplexInputChannel ReliableDuplexInputChannel { get; set; }

        protected IReliableStringMessageSender MessageSender { get; set; }
        protected IReliableStringMessageReceiver MessageReceiver { get; set; }
    }
}
