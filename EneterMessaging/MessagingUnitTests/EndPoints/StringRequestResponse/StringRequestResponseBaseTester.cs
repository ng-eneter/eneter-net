using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.EndPoints.StringMessages;
using System.Threading;

namespace Eneter.MessagingUnitTests.EndPoints.StringRequestResponse
{
    public abstract class StringRequestResponseBaseTester
    {
        protected void Setup(IMessagingSystemFactory messagingSystemFactory, string channelId)
        {
            MessagingSystemFactory = messagingSystemFactory;

            DuplexOutputChannel = MessagingSystemFactory.CreateDuplexOutputChannel(channelId);
            DuplexInputChannel = MessagingSystemFactory.CreateDuplexInputChannel(channelId);

            IDuplexStringMessagesFactory aMessageFactory = new DuplexStringMessagesFactory();
            MessageRequester = aMessageFactory.CreateDuplexStringMessageSender();
            MessageResponser = aMessageFactory.CreateDuplexStringMessageReceiver();
        }

        [Test]
        public void SendReceive_1Message()
        {
            // The test can be performed from more thread therefore we must synchronize.
            AutoResetEvent aMessageReceivedEvent = new AutoResetEvent(false);

            string aReceivedMessage = "";
            string aReceivedResponse = "";

            try
            {
                MessageResponser.RequestReceived += (x, y) =>
                {
                    aReceivedMessage = y.RequestMessage;

                    // Send the response
                    MessageResponser.SendResponseMessage(y.ResponseReceiverId, "Response");
                };
                MessageResponser.AttachDuplexInputChannel(DuplexInputChannel);

                
                MessageRequester.ResponseReceived += (x, y) =>
                    {
                        aReceivedResponse = y.ResponseMessage;

                        // Signal that the response message was received -> the loop is closed.
                        aMessageReceivedEvent.Set();
                    };
                MessageRequester.AttachDuplexOutputChannel(DuplexOutputChannel);


                MessageRequester.SendMessage("Message");

                // Wait for the signal that the message is received.
                Assert.IsTrue(aMessageReceivedEvent.WaitOne(200));
            }
            finally
            {
                MessageRequester.DetachDuplexOutputChannel();
                MessageResponser.DetachDuplexInputChannel();
            }

            // Check received values
            Assert.AreEqual("Message", aReceivedMessage);
            Assert.AreEqual("Response", aReceivedResponse);
        }

        [Test]
        public void SendReceive_MultiThreadAccess_1000Messages()
        {
            // The test can be performed from more thread therefore we must synchronize.
            AutoResetEvent aMessageReceivedEvent = new AutoResetEvent(false);

            List<string> aReceivedMessages = new List<string>();
            MessageResponser.RequestReceived += (x, y) =>
            {
                lock (aReceivedMessages)
                {
                    aReceivedMessages.Add(y.RequestMessage);
                }

                // Send the response
                MessageResponser.SendResponseMessage(y.ResponseReceiverId, "Response");
            };
            MessageResponser.AttachDuplexInputChannel(DuplexInputChannel);

            List<string> aReceivedResponses = new List<string>();
            MessageRequester.ResponseReceived += (x, y) =>
            {
                lock (aReceivedResponses)
                {
                    aReceivedResponses.Add(y.ResponseMessage);

                    if (aReceivedResponses.Count == 1000)
                    {
                        // Signal that the message was received.
                        aMessageReceivedEvent.Set();
                    }
                }
            };
            MessageRequester.AttachDuplexOutputChannel(DuplexOutputChannel);


            try
            {
                List<Thread> aThreads = new List<Thread>();

                for (int i = 0; i < 10; ++i)
                {
                    Thread aThread = new Thread(() =>
                    {
                        for (int ii = 0; ii < 100; ++ii)
                        {
                            MessageRequester.SendMessage("Message");
                            Thread.Sleep(1);
                        }
                    });

                    aThreads.Add(aThread);
                }

                aThreads.ForEach(x => x.Start());

                // Wait for the signal that the message is received.
                //Assert.IsTrue(aMessageReceivedEvent.WaitOne(1000));
                aMessageReceivedEvent.WaitOne();
            }
            finally
            {
                MessageRequester.DetachDuplexOutputChannel();
                MessageResponser.DetachDuplexInputChannel();
            }

            // Check received values
            Assert.AreEqual(1000, aReceivedMessages.Count);
            aReceivedMessages.ForEach(x => Assert.AreEqual("Message", x));

            Assert.AreEqual(1000, aReceivedResponses.Count);
            aReceivedResponses.ForEach(x => Assert.AreEqual("Response", x));
        }



        protected IMessagingSystemFactory MessagingSystemFactory { get; set; }
        protected IDuplexOutputChannel DuplexOutputChannel { get; set; }
        protected IDuplexInputChannel DuplexInputChannel { get; set; }

        protected IDuplexStringMessageSender MessageRequester { get; set; }
        protected IDuplexStringMessageReceiver MessageResponser { get; set; }
    }
}
