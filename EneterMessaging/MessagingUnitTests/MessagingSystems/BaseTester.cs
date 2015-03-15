using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using NUnit.Framework;
using System.Threading;
using Eneter.Messaging.Diagnostic;
using System.Diagnostics;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;
using Eneter.Messaging.MessagingSystems.NamedPipeMessagingSystem;
using Eneter.Messaging.MessagingSystems.SharedMemoryMessagingSystem;

namespace Eneter.MessagingUnitTests.MessagingSystems
{
    public abstract class BaseTester
    {
        public BaseTester()
        {
            ChannelId = "Channel1";
        }

        [Test]
        public virtual void Duplex_01_Send1()
        {
            SendMessageReceiveResponse(ChannelId, myRequestMessage, myResponseMessage, 1, 1, 1000, 500);
        }

        [Test]
        public virtual void Duplex_02_Send500()
        {
            SendMessageReceiveResponse(ChannelId, myRequestMessage, myResponseMessage, 1, 500, 1000, 1000);
        }

        [Test]
        public virtual void Duplex_03_Send1_10MB()
        {
            SendMessageReceiveResponse(ChannelId, myMessage_10MB, myMessage_10MB, 1, 1, 1000, 1000);
        }

        [Test]
        public virtual void Duplex_04_Send50000()
        {
            SendMessageReceiveResponse(ChannelId, myRequestMessage, myResponseMessage, 1, 50000, 500, 20000);
        }

        [Test]
        public virtual void Duplex_05_Send50_10Prallel()
        {
            SendMessageReceiveResponse(ChannelId, myRequestMessage, myResponseMessage, 10, 50, 1000, 2000);
        }

        [Test]
        public virtual void Duplex_05a_SendBroadcastResponse_50_10Clients()
        {
            SendBroadcastResponseMessage(ChannelId, myResponseMessage, 10, 50, 1000, 2000);
        }

        [Test]
        public virtual void Duplex_06_OpenCloseConnection()
        {
            ClientMock aClient = new ClientMock(MessagingSystemFactory, ChannelId);
            ServiceMock aService = new ServiceMock(MessagingSystemFactory, ChannelId);

            try
            {
                aService.InputChannel.StartListening();

                // Open the connection.
                aClient.OutputChannel.OpenConnection();
                Assert.IsTrue(aClient.OutputChannel.IsConnected);

                // handling open connection on the client side.
                EneterTrace.Info("1");
                aClient.WaitUntilConnectionOpenIsNotified(1000);
                Assert.AreEqual(aClient.OutputChannel.ChannelId, aClient.NotifiedOpenConnection.ChannelId);
                Assert.AreEqual(aClient.OutputChannel.ResponseReceiverId, aClient.NotifiedOpenConnection.ResponseReceiverId);

                // handling open connection on the service side.
                EneterTrace.Info("2");
                aService.WaitUntilResponseReceiversConnectNotified(1, 1000);
                Assert.AreEqual(1, aService.ConnectedResponseReceivers.Count());
                if (CompareResponseReceiverId)
                {
                    Assert.AreEqual(aClient.OutputChannel.ResponseReceiverId, aService.ConnectedResponseReceivers[0].ResponseReceiverId);
                }

                aClient.OutputChannel.CloseConnection();
                Assert.IsFalse(aClient.OutputChannel.IsConnected);

                EneterTrace.Info("3");
                aClient.WaitUntilConnectionClosedIsNotified(1000);
                Assert.AreEqual(aClient.OutputChannel.ChannelId, aClient.NotifiedCloseConnection.ChannelId);
                Assert.AreEqual(aClient.OutputChannel.ResponseReceiverId, aClient.NotifiedCloseConnection.ResponseReceiverId);

                EneterTrace.Info("4");
                aService.WaitUntilAllResponseReceiversDisconnectNotified(1000);
                Assert.AreEqual(1, aService.DisconnectedResponseReceivers.Count());
                if (CompareResponseReceiverId)
                {
                    Assert.AreEqual(aClient.OutputChannel.ResponseReceiverId, aService.DisconnectedResponseReceivers.First().ResponseReceiverId);
                }
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
        public virtual void Duplex_06_OpenCloseOpenSend()
        {
            ClientMock aClient = new ClientMock(MessagingSystemFactory, ChannelId);
            ServiceMock aService = new ServiceMock(MessagingSystemFactory, ChannelId);
            aService.DoOnMessageReceived_SendResponse(myResponseMessage);

            try
            {
                aService.InputChannel.StartListening();

                // Client opens the connection.
                aClient.OutputChannel.OpenConnection();
                Assert.IsTrue(aClient.OutputChannel.IsConnected);

                aClient.WaitUntilConnectionOpenIsNotified(1000);
                Assert.AreEqual(aClient.OutputChannel.ChannelId, aClient.NotifiedOpenConnection.ChannelId);
                Assert.AreEqual(aClient.OutputChannel.ResponseReceiverId, aClient.NotifiedOpenConnection.ResponseReceiverId);

                aService.WaitUntilResponseReceiversConnectNotified(1, 1000);
                Assert.AreEqual(1, aService.ConnectedResponseReceivers.Count());
                if (CompareResponseReceiverId)
                {
                    Assert.AreEqual(aClient.OutputChannel.ResponseReceiverId, aService.ConnectedResponseReceivers[0].ResponseReceiverId);
                }

                // Client closes the connection.
                aClient.OutputChannel.CloseConnection();
                Assert.IsFalse(aClient.OutputChannel.IsConnected);

                aClient.WaitUntilConnectionClosedIsNotified(1000);
                Assert.AreEqual(aClient.OutputChannel.ChannelId, aClient.NotifiedCloseConnection.ChannelId);
                Assert.AreEqual(aClient.OutputChannel.ResponseReceiverId, aClient.NotifiedCloseConnection.ResponseReceiverId);

                aService.WaitUntilAllResponseReceiversDisconnectNotified(1000);
                Assert.AreEqual(1, aService.DisconnectedResponseReceivers.Count());
                if (CompareResponseReceiverId)
                {
                    Assert.AreEqual(aClient.OutputChannel.ResponseReceiverId, aService.DisconnectedResponseReceivers[0].ResponseReceiverId);
                }

                aClient.ClearTestResults();
                aService.ClearTestResults();


                // Client opens the connection 2nd time.
                aClient.OutputChannel.OpenConnection();
                Assert.IsTrue(aClient.OutputChannel.IsConnected);

                aClient.WaitUntilConnectionOpenIsNotified(1000);
                Assert.AreEqual(aClient.OutputChannel.ChannelId, aClient.NotifiedOpenConnection.ChannelId);
                Assert.AreEqual(aClient.OutputChannel.ResponseReceiverId, aClient.NotifiedOpenConnection.ResponseReceiverId);

                aService.WaitUntilResponseReceiversConnectNotified(1, 1000);
                Assert.AreEqual(1, aService.ConnectedResponseReceivers.Count());
                if (CompareResponseReceiverId)
                {
                    Assert.AreEqual(aClient.OutputChannel.ResponseReceiverId, aService.ConnectedResponseReceivers[0].ResponseReceiverId);
                }

                // Client sends the message.
                aClient.OutputChannel.SendMessage(myRequestMessage);

                aClient.WaitUntilResponseMessagesAreReceived(1, 1000);

                Assert.AreEqual(myRequestMessage, aService.ReceivedMessages.First().Message);
                Assert.AreEqual(myResponseMessage, aClient.ReceivedMessages.First().Message);
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
        public virtual void Duplex_07_OpenConnection_if_InputChannelNotStarted()
        {
            IDuplexOutputChannel anOutputChannel = MessagingSystemFactory.CreateDuplexOutputChannel(ChannelId);

            try
            {
                anOutputChannel.OpenConnection();
            }
            catch
            {
            }

            Assert.IsFalse(anOutputChannel.IsConnected);
        }

        [Test]
        public virtual void Duplex_08_OpenFromConnectionClosed()
        {
            ClientMock aClient = new ClientMock(MessagingSystemFactory, ChannelId);

            bool anIsConnected = false;
            aClient.DoOnConnectionClosed( (x, y) =>
                {
                    anIsConnected = aClient.OutputChannel.IsConnected;

                    if (MessagingSystemFactory is SharedMemoryMessagingSystemFactory)
                    {
                        Thread.Sleep(300);
                    }

                    aClient.OutputChannel.OpenConnection();
                });

            ServiceMock aService = new ServiceMock(MessagingSystemFactory, ChannelId);

            try
            {
                aService.InputChannel.StartListening();

                // Client opens the connection.
                aClient.OutputChannel.OpenConnection();

                aClient.WaitUntilConnectionOpenIsNotified(1000);
                Assert.IsFalse(anIsConnected);

                aService.WaitUntilResponseReceiversConnectNotified(1, 1000);
                string aConnectedResponseReceiverId = aService.ConnectedResponseReceivers[0].ResponseReceiverId;

                aClient.ClearTestResults();
                aService.ClearTestResults();

                // Service disconnects the client.
                aService.InputChannel.DisconnectResponseReceiver(aConnectedResponseReceiverId);

                aService.WaitUntilResponseRecieverIdDisconnectNotified(aConnectedResponseReceiverId, 1000);
                aClient.WaitUntilConnectionClosedIsNotified(1000);
                Assert.AreEqual(aConnectedResponseReceiverId, aService.DisconnectedResponseReceivers[0].ResponseReceiverId);

                // Client should open the connection again.
                aClient.WaitUntilConnectionOpenIsNotified(1000);

                if (MessagingSystemFactory is SynchronousMessagingSystemFactory == false)
                {
                    aService.WaitUntilResponseReceiversConnectNotified(1, 1000);
                }
            }
            finally
            {
                EneterTrace.Debug("CLEANING AFTER TEST");

                aClient.OutputChannel.CloseConnection();
                aService.InputChannel.StopListening();

                // Wait for traces.
                Thread.Sleep(500);
            }
        }

        [Test]
        public virtual void Duplex_09_StopListening_SendMessage()
        {
            ClientMock aClient = new ClientMock(MessagingSystemFactory, ChannelId);
            ServiceMock aService = new ServiceMock(MessagingSystemFactory, ChannelId);

            try
            {
                aService.InputChannel.StartListening();

                aClient.OutputChannel.OpenConnection();

                aService.WaitUntilResponseReceiversConnectNotified(1, 1000);

                aService.InputChannel.StopListening();
                Assert.IsFalse(aService.InputChannel.IsListening);

                bool isSomeException = false;
                try
                {
                    // Try to send a message via the duplex output channel.
                    aClient.OutputChannel.SendMessage(myRequestMessage);
                }
                catch
                {
                    // Because the duplex input channel is not listening the sending must
                    // fail with an exception. The type of the exception depends from the type of messaging system.
                    isSomeException = true;
                }

                Assert.IsTrue(isSomeException);
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
        public virtual void Duplex_09_StopListening()
        {
            ClientMockFarm aClients = new ClientMockFarm(MessagingSystemFactory, ChannelId, 3);
            ServiceMock aService = new ServiceMock(MessagingSystemFactory, ChannelId);

            try
            {
                aService.InputChannel.StartListening();

                aClients.OpenConnectionsAsync();
                Assert.IsTrue(aClients.Clients[0].OutputChannel.IsConnected);
                Assert.IsTrue(aClients.Clients[1].OutputChannel.IsConnected);
                Assert.IsTrue(aClients.Clients[2].OutputChannel.IsConnected);

                aClients.WaitUntilAllConnectionsAreOpen(1000);
                aService.WaitUntilResponseReceiversConnectNotified(3, 1000);

                aService.InputChannel.StopListening();
                Assert.IsFalse(aService.InputChannel.IsListening);

                aService.WaitUntilAllResponseReceiversDisconnectNotified(1000);
                aClients.WaitUntilAllConnectionsAreClosed(1000);
            }
            finally
            {
                EneterTrace.Debug("CLEANING AFTER TEST");

                aClients.CloseAllConnections();
                aService.InputChannel.StopListening();

                // Wait for traces.
                Thread.Sleep(100);
            }
        }

        [Test]
        public virtual void Duplex_10_DisconnectResponseReceiver()
        {
            ClientMock aClient = new ClientMock(MessagingSystemFactory, ChannelId);
            ServiceMock aService = new ServiceMock(MessagingSystemFactory, ChannelId);

            try
            {
                aService.InputChannel.StartListening();

                aClient.OutputChannel.OpenConnection();

                aService.WaitUntilResponseReceiversConnectNotified(1, 1000);
                aClient.WaitUntilConnectionOpenIsNotified(1000);
                string aConnectedResponseReceiverId = aService.ConnectedResponseReceivers[0].ResponseReceiverId;

                aService.InputChannel.DisconnectResponseReceiver(aService.ConnectedResponseReceivers.First().ResponseReceiverId);

                aClient.WaitUntilConnectionClosedIsNotified(1000);
                aService.WaitUntilAllResponseReceiversDisconnectNotified(1000);

                Assert.AreEqual(aConnectedResponseReceiverId, aService.DisconnectedResponseReceivers[0].ResponseReceiverId);
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
        public virtual void Duplex_11_CloseConnection()
        {
            ClientMockFarm aClients = new ClientMockFarm(MessagingSystemFactory, ChannelId, 2);
            ServiceMock aService = new ServiceMock(MessagingSystemFactory, ChannelId);

            try
            {
                aService.InputChannel.StartListening();

                aClients.OpenConnectionsAsync();
                Assert.IsTrue(aClients.Clients[0].OutputChannel.IsConnected);
                Assert.IsTrue(aClients.Clients[1].OutputChannel.IsConnected);

                aClients.WaitUntilAllConnectionsAreOpen(1000);
                aService.WaitUntilResponseReceiversConnectNotified(2, 1000);
                string aResponseReceiverId1 = aService.ConnectedResponseReceivers[0].ResponseReceiverId;

                // Cient 1 closes the connection.
                aClients.Clients[0].OutputChannel.CloseConnection();
                Assert.IsFalse(aClients.Clients[0].OutputChannel.IsConnected);

                aClients.Clients[0].WaitUntilConnectionClosedIsNotified(1000);
                aService.WaitUntilResponseRecieverIdDisconnectNotified(aResponseReceiverId1, 1000);
                if (CompareResponseReceiverId)
                {
                    Assert.AreEqual(aClients.Clients[0].OutputChannel.ResponseReceiverId, aService.DisconnectedResponseReceivers[0].ResponseReceiverId);
                }

                // Client 2 can send message.
                aClients.Clients[1].OutputChannel.SendMessage(myRequestMessage);

                aService.WaitUntilMessagesAreReceived(1, 1000);

                Assert.AreEqual(myRequestMessage, aService.ReceivedMessages[0].Message);
            }
            finally
            {
                EneterTrace.Debug("CLEANING AFTER TEST");

                aClients.CloseAllConnections();
                aService.InputChannel.StopListening();

                // Wait for traces.
                Thread.Sleep(100);
            }
        }

        [Test]
        public virtual void Duplex_12_CloseFromConnectionOpened()
        {
            ClientMock aClient = new ClientMock(MessagingSystemFactory, ChannelId);

            aClient.DoOnConnectionOpen((x, y) =>
                {
                    if (MessagingSystemFactory is NamedPipeMessagingSystemFactory)
                    {
                        Thread.Sleep(500);
                    }

                    aClient.OutputChannel.CloseConnection();
                });

            ServiceMock aService = new ServiceMock(MessagingSystemFactory, ChannelId);

            try
            {
                aService.InputChannel.StartListening();

                // The ecent will try to close connection.
                aClient.OutputChannel.OpenConnection();

                aClient.WaitUntilConnectionOpenIsNotified(1000);

                if (MessagingSystemFactory is SynchronousMessagingSystemFactory == false)
                {
                    aService.WaitUntilResponseReceiversConnectNotified(1, 5000);
                }

                // Client is disconnected.
                aClient.WaitUntilConnectionClosedIsNotified(1000);

                // Client should be disconnected from the event handler.
                aService.WaitUntilAllResponseReceiversDisconnectNotified(2000);
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
        public virtual void Duplex_13_DisconnectFromResponseReceiverConnected()
        {
            ClientMock aClient = new ClientMock(MessagingSystemFactory, ChannelId);
            ServiceMock aService = new ServiceMock(MessagingSystemFactory, ChannelId);

            aService.DoOnResponseReceiverConnected((x, y) =>
                {
                    if (MessagingSystemFactory is NamedPipeMessagingSystemFactory)
                    {
                        Thread.Sleep(500);
                    }

                    aService.InputChannel.DisconnectResponseReceiver(y.ResponseReceiverId);
                });

            try
            {
                aService.InputChannel.StartListening();

                // The ecent will try to close connection.
                aClient.OutputChannel.OpenConnection();

                aClient.WaitUntilConnectionOpenIsNotified(1000);

                aService.WaitUntilAllResponseReceiversDisconnectNotified(1000);

                aClient.WaitUntilConnectionClosedIsNotified(1000);
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


        private void SendMessageReceiveResponse(string channelId, object message, object responseMessage,
                                                int numberOfClients, int numberOfMessages,
                                                int openConnectionTimeout,
                                                int allMessagesReceivedTimeout)
        {
            ThreadPool.SetMinThreads(50, 2);

            ClientMockFarm aClientFarm = new ClientMockFarm(MessagingSystemFactory, channelId, numberOfClients);

            ServiceMock aService = new ServiceMock(MessagingSystemFactory, channelId);
            aService.DoOnMessageReceived_SendResponse(responseMessage);

            try
            {
                //EneterTrace.StartProfiler();

                aService.InputChannel.StartListening();
                aClientFarm.OpenConnectionsAsync();

                aClientFarm.WaitUntilAllConnectionsAreOpen(openConnectionTimeout);
                aService.WaitUntilResponseReceiversConnectNotified(numberOfClients, openConnectionTimeout);
                Assert.AreEqual(aClientFarm.Clients.Count(), aService.ConnectedResponseReceivers.Count());

                if (CompareResponseReceiverId)
                {
                    foreach (ClientMock aClient in aClientFarm.Clients)
                    {
                        Assert.IsTrue(aService.ConnectedResponseReceivers.Any(x => x.ResponseReceiverId == aClient.OutputChannel.ResponseReceiverId));
                    }
                }

                Stopwatch aStopWatch = new Stopwatch();
                aStopWatch.Start();

                aClientFarm.SendMessageAsync(message, numberOfMessages);
                aClientFarm.WaitUntilAllResponsesAreReceived(numberOfMessages, allMessagesReceivedTimeout);

                aStopWatch.Stop();
                Console.WriteLine("Send messages to '" + ChannelId + "' completed. Elapsed time = " + aStopWatch.Elapsed);

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

        private void SendBroadcastResponseMessage(string channelId, object broadcastMessage,
                                                int numberOfClients, int numberOfMessages,
                                                int openConnectionTimeout,
                                                int allMessagesReceivedTimeout)
        {
            ThreadPool.SetMinThreads(50, 2);

            ClientMockFarm aClientFarm = new ClientMockFarm(MessagingSystemFactory, channelId, numberOfClients);
            ServiceMock aService = new ServiceMock(MessagingSystemFactory, channelId);

            try
            {
                aService.InputChannel.StartListening();
                aClientFarm.OpenConnectionsAsync();

                aClientFarm.WaitUntilAllConnectionsAreOpen(openConnectionTimeout);
                aService.WaitUntilResponseReceiversConnectNotified(numberOfClients, openConnectionTimeout);
                Assert.AreEqual(aClientFarm.Clients.Count(), aService.ConnectedResponseReceivers.Count());
                if (CompareResponseReceiverId)
                {
                    foreach (ClientMock aClient in aClientFarm.Clients)
                    {
                        Assert.IsTrue(aService.ConnectedResponseReceivers.Any(x => x.ResponseReceiverId == aClient.OutputChannel.ResponseReceiverId));
                    }
                }

                Stopwatch aStopWatch = new Stopwatch();
                aStopWatch.Start();

                for (int i = 0; i < numberOfMessages; ++i)
                {
                    aService.InputChannel.SendResponseMessage(null, broadcastMessage);
                }
                aClientFarm.WaitUntilAllResponsesAreReceived(numberOfMessages, allMessagesReceivedTimeout);

                aStopWatch.Stop();
                Console.WriteLine("Send messages to '" + ChannelId + "' completed. Elapsed time = " + aStopWatch.Elapsed);

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

        protected IMessagingSystemFactory MessagingSystemFactory { get; set; }

        protected string ChannelId { get; set; }

        protected bool CompareResponseReceiverId = true;

        protected object myRequestMessage = "Message";
        protected object myResponseMessage = "Response";

        protected object myMessage_10MB = RandomDataGenerator.GetString(10000000);
    }
}
