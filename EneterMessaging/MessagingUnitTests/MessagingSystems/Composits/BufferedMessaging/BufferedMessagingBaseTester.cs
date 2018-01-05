using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using System.Threading;
using Eneter.Messaging.MessagingSystems.Composites;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.Composites.BufferedMessagingComposit;

namespace Eneter.MessagingUnitTests.MessagingSystems.Composits.BufferedMessaging
{
    public abstract class BufferedMessagingBaseTester
    {
        [Test]
        public void A01_Send1()
        {
            SendMessageReceiveResponse(ChannelId, myRequestMessage, myResponseMessage, 1, 1, 1000, 500);
        }

        [Test]
        public void A02_Send500()
        {
            SendMessageReceiveResponse(ChannelId, myRequestMessage, myResponseMessage, 1, 500, 1000, 1000);
        }

        [Test]
        public void A03_Send1_10MB()
        {
            SendMessageReceiveResponse(ChannelId, myMessage_10MB, myMessage_10MB, 1, 1, 1000, 5000);
        }

        [Test]
        public void A04_Send50000()
        {
            SendMessageReceiveResponse(ChannelId, myRequestMessage, myResponseMessage, 1, 50000, 500, 20000);
        }

        [Test]
        public void A05_Send50_10Prallel()
        {
            SendMessageReceiveResponse(ChannelId, myRequestMessage, myResponseMessage, 10, 50, 1000, 10000);
        }

        [Test]
        public void A06_IndependentStartupOrder()
        {
            SendOfflineMessageReceiveResponse(ChannelId, myRequestMessage, myResponseMessage, 1, 1, 3000, 3000);
        }

        [Test]
        public void A07_IndependentStartupOrder50_10Parallel()
        {
            SendOfflineMessageReceiveResponse(ChannelId, myRequestMessage, myResponseMessage, 10, 50, 3000, 3000);
        }

        [Test]
        public void A08_SendMessagesOffline10()
        {
            SendOfflineMessageReceiveResponse(ChannelId, myRequestMessage, myResponseMessage, 1, 10, 3000, 3000);
        }

        [Test]
        public void A09_SendResponsesOffline()
        {
            SendOfflineResponse(ChannelId, myResponseMessage, 1, 1, 3000, 3000);
        }

        [Test]
        public void A10_SendResponsesOffline10()
        {
            SendOfflineResponse(ChannelId, myResponseMessage, 1, 10, 1000, 1000);
        }

        [Test]
        public void A11_SendResponsesOffline10_10Parallel()
        {
            SendOfflineResponse(ChannelId, myResponseMessage, 10, 10, 1000, 1000);
        }

        [Test]
        public virtual void A12_SendBroadcastResponse_50_10Clients()
        {
            SendBroadcastResponseMessage(ChannelId, myResponseMessage, 10, 50, 1000, 2000);
        }

        [Test]
        public virtual void A12_SendOfflineBroadcastResponse_50_10Clients()
        {
            SendOfflineBroadcastResponseMessage(ChannelId, myResponseMessage, 10, 50, 1000, 2000);
        }


        [Test]
        public void A13_TimeoutedResponseReceiver()
        {
            ClientMock aClient = new ClientMock(MessagingSystem, ChannelId);
            ServiceMock aService = new ServiceMock(MessagingSystem, ChannelId);

            try
            {
                aService.InputChannel.StartListening();

                // Open the connection.
                aClient.OutputChannel.OpenConnection();
                Assert.IsTrue(aClient.OutputChannel.IsConnected);

                // handling open connection on the client side.
                EneterTrace.Info("1");
                aClient.WaitUntilConnectionOpenIsNotified(2000);
                Assert.AreEqual(aClient.OutputChannel.ChannelId, aClient.NotifiedOpenConnection.ChannelId);
                Assert.AreEqual(aClient.OutputChannel.ResponseReceiverId, aClient.NotifiedOpenConnection.ResponseReceiverId);

                // handling open connection on the service side.
                EneterTrace.Info("2");
                aService.WaitUntilResponseReceiversConnectNotified(1, 1000);
                Assert.AreEqual(1, aService.ConnectedResponseReceivers.Count());
                Assert.AreEqual(aClient.OutputChannel.ResponseReceiverId, aService.ConnectedResponseReceivers[0].ResponseReceiverId);

                aClient.OutputChannel.CloseConnection();
                Assert.IsFalse(aClient.OutputChannel.IsConnected);

                // Service will disconnect the response receiver when the offline timout is exceeded.
                EneterTrace.Info("3");
                aService.WaitUntilAllResponseReceiversDisconnectNotified(2000);
                Assert.AreEqual(1, aService.DisconnectedResponseReceivers.Count());
                Assert.AreEqual(aClient.OutputChannel.ResponseReceiverId, aService.DisconnectedResponseReceivers.First().ResponseReceiverId);
             }
            finally
            {
                EneterTrace.Debug("CLEANING AFTER TEST");

                aClient.OutputChannel.CloseConnection();
                aService.InputChannel.StopListening();

                // Wait for traces.
                Thread.Sleep(100);
            }
        }

        [Test]
        public void A14_ResponseReceiverReconnects_AfterDisconnect()
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

        [Test]
        public virtual void A15_ResponseReceiverReconnects_AfterStopListening()
        {
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
                Thread.Sleep(700);

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

        [Test]
        public void A16_RequestResponse_100_ConstantlyInterrupted()
        {
            IDuplexOutputChannel aDuplexOutputChannel = MessagingSystem.CreateDuplexOutputChannel(ChannelId);
            IDuplexInputChannel aDuplexInputChannel = MessagingSystem.CreateDuplexInputChannel(ChannelId);
            IDuplexInputChannel anUnderlyingDuplexInputChannel = aDuplexInputChannel.GetField<IDuplexInputChannel>("myInputChannel");
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

        [Test]
        public void A17_Online_Offline_Events()
        {
            // Duplex output channel without queue - it will not try to reconnect.
            IBufferedDuplexOutputChannel aDuplexOutputChannel = (IBufferedDuplexOutputChannel)MessagingSystem.CreateDuplexOutputChannel(ChannelId);
            IBufferedDuplexInputChannel aDuplexInputChannel = (IBufferedDuplexInputChannel)MessagingSystem.CreateDuplexInputChannel(ChannelId);

            AutoResetEvent aConnectionsCompletedEvent = new AutoResetEvent(false);
            aDuplexInputChannel.ResponseReceiverConnected += (x, y) =>
            {
                aConnectionsCompletedEvent.Set();
            };

            AutoResetEvent aResponseReceiverOnline = new AutoResetEvent(false);
            aDuplexInputChannel.ResponseReceiverOnline += (x, y) =>
            {
                aResponseReceiverOnline.Set();
            };

            AutoResetEvent aResponseReceiverOffline = new AutoResetEvent(false);
            aDuplexInputChannel.ResponseReceiverOffline += (x, y) =>
            {
                aResponseReceiverOffline.Set();
            };

            AutoResetEvent aOnlineIsRaised = new AutoResetEvent(false);
            bool aOnlineStateAfterOnline = false;
            aDuplexOutputChannel.ConnectionOnline += (x, y) =>
            {
                aOnlineStateAfterOnline = aDuplexOutputChannel.IsOnline;
                aOnlineIsRaised.Set();
            };

            AutoResetEvent aOfflineIsRaised = new AutoResetEvent(false);
            bool aOnlineStateAfterOffline = false;
            aDuplexOutputChannel.ConnectionOffline += (x, y) =>
            {
                aOnlineStateAfterOffline = aDuplexOutputChannel.IsOnline;
                aOfflineIsRaised.Set();
            };

            try
            {
                aDuplexOutputChannel.OpenConnection();

                if (!aOfflineIsRaised.WaitOne(1000))
                {
                    Assert.Fail("Offline event was not raised.");
                }
                Assert.IsFalse(aOnlineStateAfterOffline);

                // start listening
                aDuplexInputChannel.StartListening();

                if (!aOnlineIsRaised.WaitOne(1000))
                {
                    Assert.Fail("Online event was not raised.");
                }
                Assert.IsTrue(aOnlineStateAfterOnline);

                if (!aResponseReceiverOnline.WaitOne(1000))
                {
                    Assert.Fail("ResponseReceiverOnline event was not raised.");
                }

                // Wait until the connection is open.
                if (!aConnectionsCompletedEvent.WaitOne(1000))
                {
                    Assert.Fail("Connection was not open.");
                }

                // Disconnect the response receiver.
                aDuplexInputChannel.DisconnectResponseReceiver(aDuplexOutputChannel.ResponseReceiverId);

                if (!aOfflineIsRaised.WaitOne(1000))
                {
                    Assert.Fail("Offline event was not raised after disconnection.");
                }
                Assert.IsFalse(aOnlineStateAfterOffline);

                if (aResponseReceiverOffline.WaitOne(500))
                {
                    Assert.Fail("ResponseReceiverOffline event shall NOT be raised if DisconnectResponseReceiver was called.");
                }


                // The duplex output channel will try to connect again, therefore wait until connected.
                aConnectionsCompletedEvent.WaitOne();


                if (!aOnlineIsRaised.WaitOne(1000))
                {
                    Assert.Fail("Online event was not raised after reconnection.");
                }
                Assert.IsTrue(aOnlineStateAfterOnline);

                if (!aResponseReceiverOnline.WaitOne(1000))
                {
                    Assert.Fail("ResponseReceiverOnline event was not raised.");
                }

                // duplex output channel closes the connection.
                aDuplexOutputChannel.CloseConnection();


                if (!aResponseReceiverOffline.WaitOne(1000))
                {
                    Assert.Fail("ResponseReceiverOffline event was not raised.");
                }

                if (aOfflineIsRaised.WaitOne(500))
                {
                    Assert.Fail("Offline event shall NOT be raised after CloseConnection().");
                }
                Assert.IsFalse(aOnlineStateAfterOffline);

            }
            finally
            {
                aDuplexOutputChannel.CloseConnection();
                aDuplexInputChannel.StopListening();
            }
        }


        private void SendMessageReceiveResponse(string channelId, object message, object responseMessage,
                                                int numberOfClients, int numberOfMessages,
                                                int openConnectionTimeout,
                                                int allMessagesReceivedTimeout)
        {
            ThreadPool.SetMinThreads(100, 2);

            ClientMockFarm aClientFarm = new ClientMockFarm(MessagingSystem, channelId, numberOfClients);

            ServiceMock aService = new ServiceMock(MessagingSystem, channelId);
            aService.DoOnMessageReceived_SendResponse(responseMessage);

            try
            {
                //EneterTrace.StartProfiler();

                aService.InputChannel.StartListening();
                aClientFarm.OpenConnectionsAsync();

                aClientFarm.WaitUntilAllConnectionsAreOpen(openConnectionTimeout);
                aService.WaitUntilResponseReceiversConnectNotified(numberOfClients, openConnectionTimeout);
                Assert.AreEqual(aClientFarm.Clients.Count(), aService.ConnectedResponseReceivers.Count());

                foreach (ClientMock aClient in aClientFarm.Clients)
                {
                    Assert.IsTrue(aService.ConnectedResponseReceivers.Any(x => x.ResponseReceiverId == aClient.OutputChannel.ResponseReceiverId));
                }

                PerformanceTimer aStopWatch = new PerformanceTimer();
                aStopWatch.Start();

                aClientFarm.SendMessageAsync(message, numberOfMessages);
                aClientFarm.WaitUntilAllResponsesAreReceived(numberOfMessages, allMessagesReceivedTimeout);

                aStopWatch.Stop();

                // Wait little bit more for case there is an error that more messages are sent.
                Thread.Sleep(500);

                Assert.AreEqual(numberOfMessages * numberOfClients, aService.ReceivedMessages.Count());
                Assert.AreEqual(numberOfMessages * numberOfClients, aClientFarm.ReceivedResponses.Count());
                foreach (DuplexChannelMessageEventArgs aMessage in aService.ReceivedMessages)
                {
                    Assert.AreEqual(message, aMessage.Message);
                }
                foreach (DuplexChannelMessageEventArgs aResponseMessage in aClientFarm.ReceivedResponses)
                {
                    Assert.AreEqual(responseMessage, aResponseMessage.Message);
                }
            }
            finally
            {
                EneterTrace.Debug("CLEANING AFTER TEST");

                aClientFarm.CloseAllConnections();
                aService.InputChannel.StopListening();

                //EneterTrace.StopProfiler();
                Thread.Sleep(200);
            }
        }

        private void SendOfflineMessageReceiveResponse(string channelId, object message, object responseMessage,
                                                int numberOfClients, int numberOfMessages,
                                                int openConnectionTimeout,
                                                int allMessagesReceivedTimeout)
        {
            ThreadPool.SetMinThreads(100, 2);

            ClientMockFarm aClientFarm = new ClientMockFarm(MessagingSystem, channelId, numberOfClients);

            ServiceMock aService = new ServiceMock(MessagingSystem, channelId);
            aService.DoOnMessageReceived_SendResponse(responseMessage);

            try
            {
                //EneterTrace.StartProfiler();

                aClientFarm.OpenConnectionsAsync();

                Thread.Sleep(500);

                aService.InputChannel.StartListening();
                aClientFarm.WaitUntilAllConnectionsAreOpen(openConnectionTimeout);
                aService.WaitUntilResponseReceiversConnectNotified(numberOfClients, openConnectionTimeout);
                Assert.AreEqual(aClientFarm.Clients.Count(), aService.ConnectedResponseReceivers.Count());

                foreach (ClientMock aClient in aClientFarm.Clients)
                {
                    Assert.IsTrue(aService.ConnectedResponseReceivers.Any(x => x.ResponseReceiverId == aClient.OutputChannel.ResponseReceiverId));
                }

                PerformanceTimer aStopWatch = new PerformanceTimer();
                aStopWatch.Start();

                aClientFarm.SendMessageAsync(message, numberOfMessages);
                aClientFarm.WaitUntilAllResponsesAreReceived(numberOfMessages, allMessagesReceivedTimeout);

                aStopWatch.Stop();

                // Wait little bit more for case there is an error that more messages are sent.
                Thread.Sleep(500);

                Assert.AreEqual(numberOfMessages * numberOfClients, aService.ReceivedMessages.Count());
                Assert.AreEqual(numberOfMessages * numberOfClients, aClientFarm.ReceivedResponses.Count());
                foreach (DuplexChannelMessageEventArgs aMessage in aService.ReceivedMessages)
                {
                    Assert.AreEqual(message, aMessage.Message);
                }
                foreach (DuplexChannelMessageEventArgs aResponseMessage in aClientFarm.ReceivedResponses)
                {
                    Assert.AreEqual(responseMessage, aResponseMessage.Message);
                }
            }
            finally
            {
                EneterTrace.Debug("CLEANING AFTER TEST");

                aClientFarm.CloseAllConnections();
                aService.InputChannel.StopListening();

                //EneterTrace.StopProfiler();
                Thread.Sleep(200);
            }
        }

        private void SendOfflineResponse(string channelId, object responseMessage,
                                                int numberOfClients, int numberOfMessages,
                                                int openConnectionTimeout,
                                                int allMessagesReceivedTimeout)
        {
            ThreadPool.SetMinThreads(100, 2);

            ClientMockFarm aClientFarm = new ClientMockFarm(MessagingSystem, channelId, numberOfClients);

            ServiceMock aService = new ServiceMock(MessagingSystem, channelId);
            aService.DoOnMessageReceived_SendResponse(responseMessage);

            try
            {
                //EneterTrace.StartProfiler();

                aService.InputChannel.StartListening();

                PerformanceTimer aStopWatch = new PerformanceTimer();
                aStopWatch.Start();

                foreach (ClientMock aClientMock in aClientFarm.Clients)
                {
                    for (int i = 0; i < numberOfMessages; ++i)
                    {
                        aService.InputChannel.SendResponseMessage(aClientMock.OutputChannel.ResponseReceiverId, responseMessage);
                    }
                }

                Thread.Sleep(500);

                aClientFarm.OpenConnectionsAsync();
                aClientFarm.WaitUntilAllConnectionsAreOpen(openConnectionTimeout);

                aClientFarm.WaitUntilAllResponsesAreReceived(numberOfMessages, allMessagesReceivedTimeout);

                aStopWatch.Stop();

                foreach (ClientMock aClient in aClientFarm.Clients)
                {
                    Assert.IsTrue(aService.ConnectedResponseReceivers.Any(x => x.ResponseReceiverId == aClient.OutputChannel.ResponseReceiverId));
                }

                // Wait little bit more for case there is an error that more messages are sent.
                Thread.Sleep(500);

                Assert.AreEqual(numberOfMessages * numberOfClients, aClientFarm.ReceivedResponses.Count());
                foreach (DuplexChannelMessageEventArgs aResponseMessage in aClientFarm.ReceivedResponses)
                {
                    Assert.AreEqual(responseMessage, aResponseMessage.Message);
                }
            }
            finally
            {
                EneterTrace.Debug("CLEANING AFTER TEST");

                aClientFarm.CloseAllConnections();
                aService.InputChannel.StopListening();

                //EneterTrace.StopProfiler();
                Thread.Sleep(200);
            }
        }

        private void SendBroadcastResponseMessage(string channelId, object broadcastMessage,
                                                int numberOfClients, int numberOfMessages,
                                                int openConnectionTimeout,
                                                int allMessagesReceivedTimeout)
        {
            ThreadPool.SetMinThreads(50, 2);

            ClientMockFarm aClientFarm = new ClientMockFarm(MessagingSystem, channelId, numberOfClients);
            ServiceMock aService = new ServiceMock(MessagingSystem, channelId);

            try
            {
                aService.InputChannel.StartListening();
                aClientFarm.OpenConnectionsAsync();

                aClientFarm.WaitUntilAllConnectionsAreOpen(openConnectionTimeout);
                aService.WaitUntilResponseReceiversConnectNotified(numberOfClients, openConnectionTimeout);
                Assert.AreEqual(aClientFarm.Clients.Count(), aService.ConnectedResponseReceivers.Count());
                foreach (ClientMock aClient in aClientFarm.Clients)
                {
                    Assert.IsTrue(aService.ConnectedResponseReceivers.Any(x => x.ResponseReceiverId == aClient.OutputChannel.ResponseReceiverId));
                }

                PerformanceTimer aStopWatch = new PerformanceTimer();
                aStopWatch.Start();

                for (int i = 0; i < numberOfMessages; ++i)
                {
                    aService.InputChannel.SendResponseMessage("*", broadcastMessage);
                }
                aClientFarm.WaitUntilAllResponsesAreReceived(numberOfMessages, allMessagesReceivedTimeout);

                aStopWatch.Stop();

                foreach (DuplexChannelMessageEventArgs aResponseMessage in aClientFarm.ReceivedResponses)
                {
                    Assert.AreEqual(broadcastMessage, aResponseMessage.Message);
                }
            }
            finally
            {
                EneterTrace.Debug("CLEANING AFTER TEST");

                aClientFarm.CloseAllConnections();
                aService.InputChannel.StopListening();

                //EneterTrace.StopProfiler();
                Thread.Sleep(500);
            }
        }

        private void SendOfflineBroadcastResponseMessage(string channelId, object broadcastMessage,
                                                int numberOfClients, int numberOfMessages,
                                                int openConnectionTimeout,
                                                int allMessagesReceivedTimeout)
        {
            ThreadPool.SetMinThreads(50, 2);

            ClientMockFarm aClientFarm = new ClientMockFarm(MessagingSystem, channelId, numberOfClients);
            ServiceMock aService = new ServiceMock(MessagingSystem, channelId);

            try
            {
                aService.InputChannel.StartListening();

                // Send broadcasts.
                for (int i = 0; i < numberOfMessages; ++i)
                {
                    aService.InputChannel.SendResponseMessage("*", broadcastMessage);
                }

                Thread.Sleep(500);

                aClientFarm.OpenConnectionsAsync();

                aClientFarm.WaitUntilAllConnectionsAreOpen(openConnectionTimeout);
                aService.WaitUntilResponseReceiversConnectNotified(numberOfClients, openConnectionTimeout);
                Assert.AreEqual(aClientFarm.Clients.Count(), aService.ConnectedResponseReceivers.Count());
                foreach (ClientMock aClient in aClientFarm.Clients)
                {
                    Assert.IsTrue(aService.ConnectedResponseReceivers.Any(x => x.ResponseReceiverId == aClient.OutputChannel.ResponseReceiverId));
                }

                PerformanceTimer aStopWatch = new PerformanceTimer();
                aStopWatch.Start();

                aClientFarm.WaitUntilAllResponsesAreReceived(numberOfMessages, allMessagesReceivedTimeout);

                aStopWatch.Stop();

                foreach (DuplexChannelMessageEventArgs aResponseMessage in aClientFarm.ReceivedResponses)
                {
                    Assert.AreEqual(broadcastMessage, aResponseMessage.Message);
                }
            }
            finally
            {
                EneterTrace.Debug("CLEANING AFTER TEST");

                aClientFarm.CloseAllConnections();
                aService.InputChannel.StopListening();

                //EneterTrace.StopProfiler();
                Thread.Sleep(500);
            }
        }


        protected string ChannelId { get; set; }
        protected IMessagingSystemFactory MessagingSystem { get; set; }
        protected int ConnectionInterruptionFrequency { get; set; }

        protected object myRequestMessage = "Message";
        protected object myResponseMessage = "Response";

        protected object myMessage_10MB = RandomDataGenerator.GetString(10000000);
    }
}
