﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eneter.Messaging.EndPoints.TypedMessages;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using NUnit.Framework;
using System.Threading;
using Eneter.Messaging.DataProcessing.Serializing;

namespace Eneter.MessagingUnitTests.EndPoints.TypedRequestResponse
{
    public abstract class TypedRequestResponseBaseTester
    {
        protected void Setup(IMessagingSystemFactory messagingSystemFactory, string channelId, ISerializer serializer)
        {
            MessagingSystemFactory = messagingSystemFactory;

            DuplexOutputChannel = MessagingSystemFactory.CreateDuplexOutputChannel(channelId);
            DuplexInputChannel = MessagingSystemFactory.CreateDuplexInputChannel(channelId);

            IDuplexTypedMessagesFactory aMessageFactory = new DuplexTypedMessagesFactory(serializer);
            Requester = aMessageFactory.CreateDuplexTypedMessageSender<int, int>();
            Responser = aMessageFactory.CreateDuplexTypedMessageReceiver<int, int>();
        }

        [Test]
        public void SendReceive_1Message()
        {
            // The test can be performed from more thread therefore we must synchronize.
            AutoResetEvent aMessageReceivedEvent = new AutoResetEvent(false);

            int aReceivedMessage = 0;
            Responser.MessageReceived += (x, y) =>
                {
                    aReceivedMessage = y.RequestMessage;

                    // Send the response
                    Responser.SendResponseMessage(y.ResponseReceiverId, 1000);
                };
            Responser.AttachDuplexInputChannel(DuplexInputChannel);

            int aReceivedResponse = 0;
            Requester.ResponseReceived += (x, y) =>
                {
                    aReceivedResponse = y.ResponseMessage;

                    // Signal that the response message was received -> the loop is closed.
                    aMessageReceivedEvent.Set();
                };
            Requester.AttachDuplexOutputChannel(DuplexOutputChannel);

            try
            {
                Requester.SendRequestMessage(2000);

                // Wait for the signal that the message is received.
                Assert.IsTrue(aMessageReceivedEvent.WaitOne(200));
            }
            finally
            {
                Requester.DetachDuplexOutputChannel();
                Responser.DetachDuplexInputChannel();
            }

            // Check received values
            Assert.AreEqual(2000, aReceivedMessage);
            Assert.AreEqual(1000, aReceivedResponse);
        }

        [Test]
        public void SendReceive_MultiThreadAccess_1000Messages()
        {
            // The test can be performed from more thread therefore we must synchronize.
            AutoResetEvent aMessageReceivedEvent = new AutoResetEvent(false);

            List<int> aReceivedMessages = new List<int>();
            Responser.MessageReceived += (x, y) =>
                {
                    lock (aReceivedMessages)
                    {
                        aReceivedMessages.Add(y.RequestMessage);
                    }

                    // Send the response
                    Responser.SendResponseMessage(y.ResponseReceiverId, 1000);
                };
            Responser.AttachDuplexInputChannel(DuplexInputChannel);

            List<int> aReceivedResponses = new List<int>();
            Requester.ResponseReceived += (x, y) =>
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
            Requester.AttachDuplexOutputChannel(DuplexOutputChannel);

            try
            {
                List<Thread> aThreads = new List<Thread>();

                for (int i = 0; i < 10; ++i)
                {
                    Thread aThread = new Thread(() =>
                    {
                        for (int ii = 0; ii < 100; ++ii)
                        {
                            Requester.SendRequestMessage(2000);
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
                Requester.DetachDuplexOutputChannel();
                Responser.DetachDuplexInputChannel();
            }

            // Check received values
            Assert.AreEqual(1000, aReceivedMessages.Count);
            aReceivedMessages.ForEach(x => Assert.AreEqual(2000, x));

            Assert.AreEqual(1000, aReceivedResponses.Count);
            aReceivedResponses.ForEach(x => Assert.AreEqual(1000, x));
        }



        protected IMessagingSystemFactory MessagingSystemFactory { get; set; }
        protected IDuplexOutputChannel DuplexOutputChannel { get; set; }
        protected IDuplexInputChannel DuplexInputChannel { get; set; }

        protected IDuplexTypedMessageSender<int, int> Requester { get; set; }
        protected IDuplexTypedMessageReceiver<int, int> Responser { get; set; }
    }
}
