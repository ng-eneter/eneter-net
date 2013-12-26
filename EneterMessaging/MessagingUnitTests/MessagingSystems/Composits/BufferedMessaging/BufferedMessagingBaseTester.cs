using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using System.Threading;
using Eneter.Messaging.MessagingSystems.Composites;
using Eneter.Messaging.Diagnostic;

namespace Eneter.MessagingUnitTests.MessagingSystems.Composits.BufferedMessaging
{
    public abstract class BufferedMessagingBaseTester
    {
        [Test]
        public void A01_SimpleRequestResponse()
        {
            IDuplexOutputChannel aDuplexOutputChannel = MessagingSystem.CreateDuplexOutputChannel(ChannelId);
            IDuplexInputChannel aDuplexInputChannel = MessagingSystem.CreateDuplexInputChannel(ChannelId);


            // Received messages.
            List<int> aReceivedMessages = new List<int>();
            aDuplexInputChannel.MessageReceived += (x, y) =>
            {
                //EneterTrace.Info("Message Received");
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
                //EneterTrace.Info("Response Received");
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
        public void A02_IndependentStartupOrder()
        {
            IDuplexOutputChannel aDuplexOutputChannel = MessagingSystem.CreateDuplexOutputChannel(ChannelId);
            IDuplexInputChannel aDuplexInputChannel = MessagingSystem.CreateDuplexInputChannel(ChannelId);


            // Received messages.
            List<int> aReceivedMessages = new List<int>();
            aDuplexInputChannel.MessageReceived += (x, y) =>
            {
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
                aDuplexOutputChannel.OpenConnection();

                Thread.Sleep(500);

                aDuplexInputChannel.StartListening();
                

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
        public void A04_SendMessagesOffline()
        {
            IDuplexOutputChannel aDuplexOutputChannel = MessagingSystem.CreateDuplexOutputChannel(ChannelId);
            IDuplexInputChannel aDuplexInputChannel = MessagingSystem.CreateDuplexInputChannel(ChannelId);


            // Received messages.
            List<int> aReceivedMessages = new List<int>();
            aDuplexInputChannel.MessageReceived += (x, y) =>
                {
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
                aDuplexOutputChannel.OpenConnection();

                for (int i = 0; i < 20; ++i)
                {
                    aDuplexOutputChannel.SendMessage(i.ToString());
                }

                Thread.Sleep(500);

                aDuplexInputChannel.StartListening();

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
        public void A05_SendResponsesOffline()
        {
            IDuplexOutputChannel aDuplexOutputChannel = MessagingSystem.CreateDuplexOutputChannel(ChannelId, "MyResponseReceiverId");
            IDuplexInputChannel aDuplexInputChannel = MessagingSystem.CreateDuplexInputChannel(ChannelId);


            // Received response messages.
            List<int> aReceivedResponseMessages = new List<int>();
            AutoResetEvent anAllMessagesProcessedEvent = new AutoResetEvent(false);
            aDuplexOutputChannel.ResponseMessageReceived += (x, y) =>
                {
                    lock (aReceivedResponseMessages)
                    {
                        string aReceivedMessage = y.Message as string;
                        aReceivedResponseMessages.Add(int.Parse(aReceivedMessage));

                        if (aReceivedResponseMessages.Count == 20)
                        {
                            anAllMessagesProcessedEvent.Set();
                        }
                    }
                };


            try
            {
                aDuplexInputChannel.StartListening();

                // Send messages to the response receiver, that is not connected.
                for (int i = 0; i < 20; ++i)
                {
                    aDuplexInputChannel.SendResponseMessage("MyResponseReceiverId", i.ToString());
                }

                Thread.Sleep(500);

                aDuplexOutputChannel.OpenConnection();

                // Wait untill all messages are processed.
                anAllMessagesProcessedEvent.WaitOne();

            }
            finally
            {
                aDuplexOutputChannel.CloseConnection();
                aDuplexInputChannel.StopListening();
            }

            aReceivedResponseMessages.Sort();
            Assert.AreEqual(20, aReceivedResponseMessages.Count);
            for (int i = 0; i < 20; ++i)
            {
                Assert.AreEqual(i, aReceivedResponseMessages[i]);
            }


        }

        [Test]
        public void A06_TimeoutedResponseReceiver()
        {
            // Duplex output channel without queue - it will not try to reconnect.
            IDuplexOutputChannel aDuplexOutputChannel = UnderlyingMessaging.CreateDuplexOutputChannel(ChannelId);
            IDuplexInputChannel aDuplexInputChannel = MessagingSystem.CreateDuplexInputChannel(ChannelId);

            AutoResetEvent aConnectionClosedEvent = new AutoResetEvent(false);
            aDuplexOutputChannel.ConnectionClosed += (x, y) =>
                {
                    aConnectionClosedEvent.Set();
                };

            string aDisconnectedResponseReceiverId = "";
            AutoResetEvent aResponseReceiverDisconnectedEvent = new AutoResetEvent(false);
            aDuplexInputChannel.ResponseReceiverDisconnected += (x, y) =>
                {
                    aDisconnectedResponseReceiverId = y.ResponseReceiverId;
                    aResponseReceiverDisconnectedEvent.Set();
                };

            try
            {
                aDuplexInputChannel.StartListening();
                aDuplexOutputChannel.OpenConnection();

                // Disconnect the output channel.
                // The input channel should disconnect the client after max offline time.
                aDuplexOutputChannel.CloseConnection();

                aConnectionClosedEvent.WaitOne();
                aResponseReceiverDisconnectedEvent.WaitOne();
            }
            finally
            {
                aDuplexOutputChannel.CloseConnection();
                aDuplexInputChannel.StopListening();
            }

        }

        [Test]
        public void A07_ResponseReceiverReconnects_AfterDisconnect()
        {
            // Duplex output channel without queue - it will not try to reconnect.
            IDuplexOutputChannel aDuplexOutputChannel = MessagingSystem.CreateDuplexOutputChannel(ChannelId);
            IDuplexInputChannel aDuplexInputChannel = MessagingSystem.CreateDuplexInputChannel(ChannelId);

            AutoResetEvent aConnectionsCompletedEvent = new AutoResetEvent(false);
            List<string> anOpenConnections = new List<string>();
            aDuplexInputChannel.ResponseReceiverConnected += (x, y) =>
                {
                    lock (anOpenConnections)
                    {
                        anOpenConnections.Add(y.ResponseReceiverId);

                        aConnectionsCompletedEvent.Set();
                    }
                };

            try
            {
                aDuplexInputChannel.StartListening();
                aDuplexOutputChannel.OpenConnection();

                // Wait until the connection is open.
                aConnectionsCompletedEvent.WaitOne();

                // Disconnect the response receiver.
                aDuplexInputChannel.DisconnectResponseReceiver(aDuplexOutputChannel.ResponseReceiverId);

                // The duplex output channel will try to connect again, therefore wait until connected.
                aConnectionsCompletedEvent.WaitOne();
            }
            finally
            {
                aDuplexOutputChannel.CloseConnection();
                aDuplexInputChannel.StopListening();
            }


            Assert.AreEqual(2, anOpenConnections.Count);

            // Both connections should be same.
            Assert.AreEqual(aDuplexOutputChannel.ResponseReceiverId, anOpenConnections[0]);
            Assert.AreEqual(aDuplexOutputChannel.ResponseReceiverId, anOpenConnections[1]);
        }

        // Note: This test is not applicable for synchronous messaging.
        [Test]
        public virtual void A08_ResponseReceiverReconnects_AfterStopListening()
        {
            // Duplex output channel without queue - it will not try to reconnect.
            IDuplexOutputChannel aDuplexOutputChannel = MessagingSystem.CreateDuplexOutputChannel(ChannelId);
            IDuplexInputChannel aDuplexInputChannel = MessagingSystem.CreateDuplexInputChannel(ChannelId);

            AutoResetEvent aConnectionsCompletedEvent = new AutoResetEvent(false);
            List<string> anOpenConnections = new List<string>();
            aDuplexInputChannel.ResponseReceiverConnected += (x, y) =>
                {
                    lock (anOpenConnections)
                    {
                        anOpenConnections.Add(y.ResponseReceiverId);
                        aConnectionsCompletedEvent.Set();
                    }
                };

            try
            {
                aDuplexInputChannel.StartListening();
                aDuplexOutputChannel.OpenConnection();

                // Wait until the client is connected.
                aConnectionsCompletedEvent.WaitOne();

                // Stop listenig.
                aDuplexInputChannel.StopListening();

                // Give some time to stop.
                Thread.Sleep(300);

                // Start listening again.
                aDuplexInputChannel.StartListening();

                // The duplex output channel will try to connect again, therefore wait until connected.
                aConnectionsCompletedEvent.WaitOne();

                Assert.IsTrue(aDuplexOutputChannel.IsConnected);
            }
            finally
            {
                aDuplexOutputChannel.CloseConnection();
                aDuplexInputChannel.StopListening();
            }


            Assert.AreEqual(2, anOpenConnections.Count);

            // Both connections should be same.
            Assert.AreEqual(aDuplexOutputChannel.ResponseReceiverId, anOpenConnections[0]);
            Assert.AreEqual(aDuplexOutputChannel.ResponseReceiverId, anOpenConnections[1]);
        }

#if !COMPACT_FRAMEWORK
        [Test]
        public void A09_RequestResponse_100_ConstantlyInterrupted()
        {
            IDuplexOutputChannel aDuplexOutputChannel = MessagingSystem.CreateDuplexOutputChannel(ChannelId);
            IDuplexInputChannel aDuplexInputChannel = MessagingSystem.CreateDuplexInputChannel(ChannelId);
            IDuplexInputChannel anUnderlyingDuplexInputChannel = aDuplexInputChannel.GetField<IDuplexInputChannel>("myUnderlyingInputChannel");
            Assert.NotNull(anUnderlyingDuplexInputChannel);

            AutoResetEvent anAllMessagesProcessedEvent = new AutoResetEvent(false);

            // Received messages.
            List<int> aReceivedMessages = new List<int>();
            aDuplexInputChannel.MessageReceived += (x, y) =>
                {
                    lock (aReceivedMessages)
                    {
                        string aReceivedMessage = y.Message as string;

                        EneterTrace.Info("Received message: " + aReceivedMessage);

                        int k = int.Parse(aReceivedMessage);
                        aReceivedMessages.Add(k);
                        k += 1000;

                        EneterTrace.Info("Sent response message: " + k.ToString());
                        aDuplexInputChannel.SendResponseMessage(y.ResponseReceiverId, k.ToString());
                    }
                };

            aDuplexOutputChannel.ConnectionClosed += (x, y) =>
                {
                    EneterTrace.Info("ConnectionClosed invoked in duplex output channel");

                    // The buffered duplex output channel exceeded the max offline time.
                    anAllMessagesProcessedEvent.Set();
                };

            // Received response messages.
            List<int> aReceivedResponseMessages = new List<int>();
            aDuplexOutputChannel.ResponseMessageReceived += (x, y) =>
                {
                    lock (aReceivedResponseMessages)
                    {
                        string aReceivedMessage = y.Message as string;

                        EneterTrace.Info("Received response message: " + aReceivedMessage);

                        int k = int.Parse(aReceivedMessage);
                        aReceivedResponseMessages.Add(k);

                        if (aReceivedResponseMessages.Count == 100)
                        {
                            anAllMessagesProcessedEvent.Set();
                        }
                    }
                };


            try
            {
                bool aTestFinishedFlag = false;

                aDuplexInputChannel.StartListening();
                aDuplexOutputChannel.OpenConnection();


                Thread anInteruptingThread = new Thread(() =>
                    {
                        for (int i = 0; i < 100 && !aTestFinishedFlag; ++i)
                        {
                            anUnderlyingDuplexInputChannel.DisconnectResponseReceiver(aDuplexOutputChannel.ResponseReceiverId);
                            Thread.Sleep(ConnectionInterruptionFrequency);
                        }
                    });

                // Start constant disconnecting.
                anInteruptingThread.Start();

                for (int i = 0; i < 100; ++i)
                {
                    aDuplexOutputChannel.SendMessage(i.ToString());
                }

                // Wait until all messages are processed.
                //anAllMessagesProcessedEvent.WaitOne();
                Assert.IsTrue(anAllMessagesProcessedEvent.WaitOne(20000), "The timeout occured.");

                aTestFinishedFlag = true;
                anInteruptingThread.Join();
            }
            finally
            {
                aDuplexOutputChannel.CloseConnection();
                aDuplexInputChannel.StopListening();
            }

            aReceivedMessages.Sort();
            Assert.AreEqual(100, aReceivedMessages.Count);
            for (int i = 0; i < 100; ++i)
            {
                Assert.AreEqual(i, aReceivedMessages[i]);
            }

            aReceivedResponseMessages.Sort();
            Assert.AreEqual(100, aReceivedResponseMessages.Count);
            for (int i = 0; i < 100; ++i)
            {
                Assert.AreEqual(i + 1000, aReceivedResponseMessages[i]);
            }
        }
#endif


        protected string ChannelId { get; set; }
        protected IMessagingSystemFactory UnderlyingMessaging { get; set; }
        protected IMessagingSystemFactory MessagingSystem { get; set; }
        protected int ConnectionInterruptionFrequency { get; set; }
    }
}
