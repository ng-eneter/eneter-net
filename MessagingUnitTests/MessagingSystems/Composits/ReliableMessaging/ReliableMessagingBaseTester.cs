using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.Composites.ReliableMessagingComposit;
using System.Threading;
using Eneter.Messaging.Diagnostic;

namespace Eneter.MessagingUnitTests.MessagingSystems.Composits.ReliableMessaging
{
    public abstract class ReliableMessagingBaseTester
    {
        [Test]
        public void A01_NotReliable_RequestResponse()
        {
            IDuplexInputChannel aDuplexInputChannel = ReliableMessagingFactory.CreateDuplexInputChannel(ChannelId);
            IDuplexOutputChannel aDuplexOutputChannel = ReliableMessagingFactory.CreateDuplexOutputChannel(ChannelId);

            // Received messages.
            List<int> aReceivedMessages = new List<int>();
            aDuplexInputChannel.MessageReceived += (x, y) =>
                {
                    EneterTrace.Info("bbbb");

                    lock (aReceivedMessages)
                    {
                        string aReceivedMessage = y.Message as string;

                        int k = int.Parse(aReceivedMessage);
                        aReceivedMessages.Add(k);
                        k += 1000;

                        aDuplexInputChannel.SendResponseMessage(y.ResponseReceiverId, k.ToString());
                    }
                };

            // Received response messages.
            List<int> aReceivedResponseMessages = new List<int>();
            AutoResetEvent anAllMessagesProcessedEvent = new AutoResetEvent(false);
            aDuplexOutputChannel.ResponseMessageReceived += (x, y) =>
                {
                    EneterTrace.Info("aaaa");

                    lock (aReceivedResponseMessages)
                    {
                        string aReceivedMessage = y.Message as string;

                        int k = int.Parse(aReceivedMessage);
                        aReceivedResponseMessages.Add(k);

                        if (k == 1019)
                        {
                            anAllMessagesProcessedEvent.Set();
                        }
                    }
                };


            try
            {
                aDuplexInputChannel.StartListening();
                aDuplexOutputChannel.OpenConnection();

                for (int i = 0; i < 20; ++i)
                {
                    aDuplexOutputChannel.SendMessage(i.ToString());
                }

                // Wait untill all messages are processed.
                anAllMessagesProcessedEvent.WaitOne();

            }
            finally
            {
                aDuplexOutputChannel.CloseConnection();
                aDuplexInputChannel.StopListening();
            }

            aReceivedMessages.Sort();
            Assert.AreEqual(20, aReceivedMessages.Count);
            for (int i = 0; i < 20; ++i)
            {
                Assert.AreEqual(i, aReceivedMessages[i]);
            }

            aReceivedResponseMessages.Sort();
            Assert.AreEqual(20, aReceivedResponseMessages.Count);
            for (int i = 0; i < 20; ++i)
            {
                Assert.AreEqual(i + 1000, aReceivedResponseMessages[i]);
            }
        }

        [Test]
        public void A02_Reliable_RequestResponse()
        {
            IReliableDuplexInputChannel aReliableDuplexInputChannel = ReliableMessagingFactory.CreateDuplexInputChannel(ChannelId);
            IReliableDuplexOutputChannel aReliableDuplexOutputChannel = ReliableMessagingFactory.CreateDuplexOutputChannel(ChannelId);

            // Received messages.
            List<int> aReceivedMessages = new List<int>();
            List<string> aSentResponseMessages = new List<string>();
            aReliableDuplexInputChannel.MessageReceived += (x, y) =>
                {
                    lock (aReceivedMessages)
                    {
                        string aReceivedMessage = y.Message as string;

                        EneterTrace.Info("Message received " + aReceivedMessage);

                        int k = int.Parse(aReceivedMessage);
                        aReceivedMessages.Add(k);
                        k += 1000;

                        string aResponseMessageId = aReliableDuplexInputChannel.SendResponseMessage(y.ResponseReceiverId, k.ToString());
                        aSentResponseMessages.Add(aResponseMessageId);

                        EneterTrace.Info("Response message sent " + k.ToString() + " " + aResponseMessageId);
                    }
                };

            aReliableDuplexInputChannel.ResponseReceiverConnected += (x, y) =>
                {
                    EneterTrace.Info("Response receiver connected.");
                };
            aReliableDuplexInputChannel.ResponseReceiverDisconnected += (x, y) =>
                {
                    EneterTrace.Info("Response receiver disconnected.");
                };


            // Acknowledged response messages.
            AutoResetEvent anAllMessagesProcessedEvent = new AutoResetEvent(false);
            List<string> anAcknowledgedResponseMessages = new List<string>();
            aReliableDuplexInputChannel.ResponseMessageDelivered += (x, y) =>
                {
                    lock (anAcknowledgedResponseMessages)
                    {
                        anAcknowledgedResponseMessages.Add(y.MessageId);

                        EneterTrace.Info("Response message delivered " + y.MessageId);

                        if (anAcknowledgedResponseMessages.Count == 20)
                        {
                            anAllMessagesProcessedEvent.Set();
                        }
                    }
                };

            // Received response messages.
            List<int> aReceivedResponseMessages = new List<int>();
            aReliableDuplexOutputChannel.ResponseMessageReceived += (x, y) =>
                {
                    lock (aReceivedResponseMessages)
                    {
                        string aReceivedMessage = y.Message as string;

                        EneterTrace.Info("Response message received " + aReceivedMessage);

                        int k = int.Parse(aReceivedMessage);
                        aReceivedResponseMessages.Add(k);
                    }
                };

            // Acknowledged messages.
            List<string> anAcknowledgedMessages = new List<string>();
            aReliableDuplexOutputChannel.MessageDelivered += (x, y) =>
                {
                    lock (anAcknowledgedMessages)
                    {
                        EneterTrace.Info("Message delivered " + y.MessageId);

                        anAcknowledgedMessages.Add(y.MessageId);
                    }
                };

            aReliableDuplexOutputChannel.ConnectionOpened += (x, y) =>
                {
                    EneterTrace.Info("Connection opned.");
                };

            aReliableDuplexOutputChannel.ConnectionClosed += (x, y) =>
                {
                    EneterTrace.Info("Connection closed.");
                };

            List<string> aSentMessages = new List<string>();

            try
            {
                aReliableDuplexInputChannel.StartListening();
                aReliableDuplexOutputChannel.OpenConnection();

                for (int i = 0; i < 20; ++i)
                {
                    string aMessageId = aReliableDuplexOutputChannel.SendMessage(i.ToString());
                    aSentMessages.Add(aMessageId);

                    EneterTrace.Info("Message sent " + i.ToString() + " " + aMessageId);
                }

                // Wait untill all messages are processed.
                anAllMessagesProcessedEvent.WaitOne();

            }
            finally
            {
                EneterTrace.Info("Finally block");

                aReliableDuplexOutputChannel.CloseConnection();
                aReliableDuplexInputChannel.StopListening();
            }

            aReceivedMessages.Sort();
            Assert.AreEqual(20, aReceivedMessages.Count);
            for (int i = 0; i < 20; ++i)
            {
                Assert.AreEqual(i, aReceivedMessages[i]);
            }

            aReceivedResponseMessages.Sort();
            Assert.AreEqual(20, aReceivedResponseMessages.Count);
            for (int i = 0; i < 20; ++i)
            {
                Assert.AreEqual(i + 1000, aReceivedResponseMessages[i]);
            }

            aSentMessages.Sort();
            anAcknowledgedMessages.Sort();
            Assert.IsTrue(aSentMessages.SequenceEqual(anAcknowledgedMessages));

            aSentResponseMessages.Sort();
            anAcknowledgedResponseMessages.Sort();
            Assert.IsTrue(aSentResponseMessages.SequenceEqual(anAcknowledgedResponseMessages));
        }

        [Test]
        public void A03_Reliable_RequestResponse_WithNonReliableBetween()
        {
            // Final receiver
            IReliableDuplexInputChannel aReliableDuplexInputChannel = ReliableMessagingFactory.CreateDuplexInputChannel(ChannelId);
            
            // Between sender
            IDuplexOutputChannel aDuplexOutputChannel = UnderlyingMessaging.CreateDuplexOutputChannel(ChannelId);

            // Between receiver
            IDuplexInputChannel aDuplexInputChannel = UnderlyingMessaging.CreateDuplexInputChannel(ChannelId2);

            // First sender
            IReliableDuplexOutputChannel aReliableDuplexOutputChannel = ReliableMessagingFactory.CreateDuplexOutputChannel(ChannelId2);
            

            // Finaly received messages.
            List<int> aReceivedMessages = new List<int>();
            List<string> aSentResponseMessages = new List<string>();
            aReliableDuplexInputChannel.MessageReceived += (x, y) =>
                {
                    lock (aReceivedMessages)
                    {
                        string aReceivedMessage = y.Message as string;

                        EneterTrace.Info("Message received " + aReceivedMessage);

                        int k = int.Parse(aReceivedMessage);
                        aReceivedMessages.Add(k);
                        k += 1000;

                        string aResponseMessageId = aReliableDuplexInputChannel.SendResponseMessage(y.ResponseReceiverId, k.ToString());
                        aSentResponseMessages.Add(aResponseMessageId);

                        EneterTrace.Info("Response message sent " + k.ToString() + " " + aResponseMessageId);
                    }
                };

            // Between received message by non-reliable duplex input channel and forwarded by non-reliable duplex output channel.
            aDuplexInputChannel.MessageReceived += (x, y) =>
                {
                    EneterTrace.Info("MIDDLE message received " + y.Message as string);

                    aDuplexOutputChannel.SendMessage(y.Message);
                };

            // Between received response message by non-reliable duplex output channel and forwarded by non-reliable duplex input channel.
            aDuplexOutputChannel.ResponseMessageReceived += (x, y) =>
                {
                    EneterTrace.Info("MIDDLE Response message received " + y.Message as string);

                    aDuplexInputChannel.SendResponseMessage(aReliableDuplexOutputChannel.ResponseReceiverId, y.Message);
                };


            // Acknowledged response messages.
            AutoResetEvent anAllMessagesProcessedEvent = new AutoResetEvent(false);
            List<string> anAcknowledgedResponseMessages = new List<string>();
            aReliableDuplexInputChannel.ResponseMessageDelivered += (x, y) =>
            {
                lock (anAcknowledgedResponseMessages)
                {
                    anAcknowledgedResponseMessages.Add(y.MessageId);

                    EneterTrace.Info("Response message delivered " + y.MessageId);

                    if (anAcknowledgedResponseMessages.Count == 20)
                    {
                        anAllMessagesProcessedEvent.Set();
                    }
                }
            };

            // Received response messages.
            List<int> aReceivedResponseMessages = new List<int>();
            aReliableDuplexOutputChannel.ResponseMessageReceived += (x, y) =>
            {
                lock (aReceivedResponseMessages)
                {
                    string aReceivedMessage = y.Message as string;

                    EneterTrace.Info("Response message received " + aReceivedMessage);

                    int k = int.Parse(aReceivedMessage);
                    aReceivedResponseMessages.Add(k);
                }
            };

            // Acknowledged messages.
            List<string> anAcknowledgedMessages = new List<string>();
            aReliableDuplexOutputChannel.MessageDelivered += (x, y) =>
            {
                lock (anAcknowledgedMessages)
                {
                    EneterTrace.Info("Message delivered " + y.MessageId);

                    anAcknowledgedMessages.Add(y.MessageId);
                }
            };

            List<string> aSentMessages = new List<string>();

            try
            {
                aReliableDuplexInputChannel.StartListening();
                aDuplexInputChannel.StartListening();
                aDuplexOutputChannel.OpenConnection();
                aReliableDuplexOutputChannel.OpenConnection();

                for (int i = 0; i < 20; ++i)
                {
                    string aMessageId = aReliableDuplexOutputChannel.SendMessage(i.ToString());
                    aSentMessages.Add(aMessageId);

                    EneterTrace.Info("Message sent " + i.ToString() + " " + aMessageId);
                }

                // Wait untill all messages are processed.
                anAllMessagesProcessedEvent.WaitOne();

            }
            finally
            {
                EneterTrace.Info("Finally block");

                aReliableDuplexOutputChannel.CloseConnection();
                aDuplexInputChannel.StopListening();
                aDuplexOutputChannel.CloseConnection();
                aReliableDuplexInputChannel.StopListening();
            }

            aReceivedMessages.Sort();
            Assert.AreEqual(20, aReceivedMessages.Count);
            for (int i = 0; i < 20; ++i)
            {
                Assert.AreEqual(i, aReceivedMessages[i]);
            }

            aReceivedResponseMessages.Sort();
            Assert.AreEqual(20, aReceivedResponseMessages.Count);
            for (int i = 0; i < 20; ++i)
            {
                Assert.AreEqual(i + 1000, aReceivedResponseMessages[i]);
            }

            aSentMessages.Sort();
            anAcknowledgedMessages.Sort();
            Assert.IsTrue(aSentMessages.SequenceEqual(anAcknowledgedMessages));

            aSentResponseMessages.Sort();
            anAcknowledgedResponseMessages.Sort();
            Assert.IsTrue(aSentResponseMessages.SequenceEqual(anAcknowledgedResponseMessages));
        }

        [Test]
        public void A04_MessageNotDelivered()
        {
            IDuplexInputChannel aReliableDuplexInputChannel = UnderlyingMessaging.CreateDuplexInputChannel(ChannelId);
            IReliableDuplexOutputChannel aReliableDuplexOutputChannel = ReliableMessagingFactory.CreateDuplexOutputChannel(ChannelId);

            AutoResetEvent anAllMessagesProcessedEvent = new AutoResetEvent(false);
            List<string> aNotDeliveredMessages = new List<string>();
            aReliableDuplexOutputChannel.MessageNotDelivered += (x, y) =>
                {
                    lock (aNotDeliveredMessages)
                    {
                        aNotDeliveredMessages.Add(y.MessageId);

                        if (aNotDeliveredMessages.Count == 20)
                        {
                            anAllMessagesProcessedEvent.Set();
                        }
                    }
                };

            List<string> aSentMessages = new List<string>();

            try
            {
                aReliableDuplexInputChannel.StartListening();
                aReliableDuplexOutputChannel.OpenConnection();

                for (int i = 0; i < 20; ++i)
                {
                    string aMessageId = aReliableDuplexOutputChannel.SendMessage(i.ToString());
                    aSentMessages.Add(aMessageId);
                }

                // Wait untill all messages are processed.
                anAllMessagesProcessedEvent.WaitOne();

            }
            finally
            {
                aReliableDuplexOutputChannel.CloseConnection();
                aReliableDuplexInputChannel.StopListening();
            }

            aSentMessages.Sort();
            aNotDeliveredMessages.Sort();
            Assert.IsTrue(aSentMessages.SequenceEqual(aNotDeliveredMessages));

            Assert.AreEqual(20, aSentMessages.Count);
        }

        [Test]
        public void A05_ResponseMessageNotDelivered()
        {
            // Final receiver
            IReliableDuplexInputChannel aReliableDuplexInputChannel = ReliableMessagingFactory.CreateDuplexInputChannel(ChannelId);

            // Between sender
            IDuplexOutputChannel aDuplexOutputChannel = UnderlyingMessaging.CreateDuplexOutputChannel(ChannelId);

            // Between receiver
            IDuplexInputChannel aDuplexInputChannel = UnderlyingMessaging.CreateDuplexInputChannel(ChannelId2);

            // First sender
            IReliableDuplexOutputChannel aReliableDuplexOutputChannel = ReliableMessagingFactory.CreateDuplexOutputChannel(ChannelId2);


            // Finaly received messages.
            List<int> aReceivedMessages = new List<int>();
            List<string> aSentResponseMessageIds = new List<string>();
            aReliableDuplexInputChannel.MessageReceived += (x, y) =>
            {
                lock (aReceivedMessages)
                {
                    string aReceivedMessage = y.Message as string;

                    int k = int.Parse(aReceivedMessage);
                    aReceivedMessages.Add(k);
                    k += 1000;

                    string aResponseMessageId = aReliableDuplexInputChannel.SendResponseMessage(y.ResponseReceiverId, k.ToString());
                    aSentResponseMessageIds.Add(aResponseMessageId);
                }
            };

            // Between received message by non-reliable duplex input channel and forwarded by non-reliable duplex output channel.
            aDuplexInputChannel.MessageReceived += (x, y) =>
            {
                aDuplexOutputChannel.SendMessage(y.Message);
            };

            // Between received response message by non-reliable duplex output channel and forwarded by non-reliable duplex input channel.
            aDuplexOutputChannel.ResponseMessageReceived += (x, y) =>
            {
                // DO NOTHING - therefore the response will not be forwarded.
            };

            // Delivered response messages ==> should be 20
            List<string> aDeliveredResponseMessageIds = new List<string>();
            aReliableDuplexInputChannel.ResponseMessageDelivered += (x, y) =>
                {
                    lock (aDeliveredResponseMessageIds)
                    {
                        aDeliveredResponseMessageIds.Add(y.MessageId);
                    }
                };

            // Observe not received response messages ==> should be 20
            AutoResetEvent anAllMessagesProcessedEvent = new AutoResetEvent(false);
            List<string> aNotDeliveredResponseMessageIds = new List<string>();
            aReliableDuplexInputChannel.ResponseMessageNotDelivered += (x, y) =>
            {
                lock (aNotDeliveredResponseMessageIds)
                {
                    aNotDeliveredResponseMessageIds.Add(y.MessageId);

                    if (aNotDeliveredResponseMessageIds.Count == 20)
                    {
                        anAllMessagesProcessedEvent.Set();
                    }
                }
            };

            // Acknowledged messages ==> should be 0
            List<string> aDeliveredMessageIds = new List<string>();
            aReliableDuplexOutputChannel.MessageDelivered += (x, y) =>
            {
                lock (aDeliveredMessageIds)
                {
                    aDeliveredMessageIds.Add(y.MessageId);
                }
            };

            // Not delivered messages ==> should be 20
            List<string> aNotDeliveredMessageIds = new List<string>();
            aReliableDuplexOutputChannel.MessageNotDelivered += (x, y) =>
                {
                    lock (aNotDeliveredMessageIds)
                    {
                        aNotDeliveredMessageIds.Add(y.MessageId);
                    }
                };


            List<string> aSentMessageIds = new List<string>();

            try
            {
                aReliableDuplexInputChannel.StartListening();
                aDuplexInputChannel.StartListening();
                aDuplexOutputChannel.OpenConnection();
                aReliableDuplexOutputChannel.OpenConnection();

                for (int i = 0; i < 20; ++i)
                {
                    string aMessageId = aReliableDuplexOutputChannel.SendMessage(i.ToString());
                    aSentMessageIds.Add(aMessageId);
                }

                // Wait untill all messages are processed.
                anAllMessagesProcessedEvent.WaitOne();

            }
            finally
            {
                aReliableDuplexOutputChannel.CloseConnection();
                aDuplexInputChannel.StopListening();
                aDuplexOutputChannel.CloseConnection();
                aReliableDuplexInputChannel.StopListening();
            }


            Assert.AreEqual(20, aSentMessageIds.Count);
            
            aSentMessageIds.Sort();
            aNotDeliveredMessageIds.Sort();
            Assert.IsTrue(aSentMessageIds.SequenceEqual(aNotDeliveredMessageIds));
            
            // The communication is interrupted in the middle, therefore the duplex output channel will not get the acknowledgement.
            Assert.AreEqual(0, aDeliveredMessageIds.Count);

            aReceivedMessages.Sort();
            Assert.AreEqual(20, aReceivedMessages.Count);
            for (int i = 0; i < 20; ++i)
            {
                Assert.AreEqual(i, aReceivedMessages[i]);
            }

            Assert.AreEqual(20, aSentResponseMessageIds.Count);
            
            aSentResponseMessageIds.Sort();
            aNotDeliveredResponseMessageIds.Sort();
            Assert.IsTrue(aSentResponseMessageIds.SequenceEqual(aNotDeliveredResponseMessageIds));

            Assert.AreEqual(0, aDeliveredResponseMessageIds.Count);
        }


        protected IMessagingSystemFactory UnderlyingMessaging { get; set; }
        protected IReliableMessagingFactory ReliableMessagingFactory { get; set; }

        protected string ChannelId { get; set; }
        protected string ChannelId2 { get; set; }
    }
}
