using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using NUnit.Framework;
using System.Threading;
using Eneter.Messaging.Diagnostic;
using System.Diagnostics;

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
            SendMessageReceiveResponse(ChannelId, "Message", "Response", 1, 1, 500, 500);
        }

        [Test]
        public virtual void Duplex_02_Send500()
        {
            SendMessageReceiveResponse(ChannelId, "Message", "Respones", 1, 500, 1000, 1000);
        }

        [Test]
        public virtual void Duplex_03_Send1_10MB()
        {
            SendMessageReceiveResponse(ChannelId, myMessage_10MB, myMessage_10MB, 1, 1, 1000, 1000);
        }

        [Test]
        public virtual void Duplex_04_Send50000()
        {
            SendMessageReceiveResponse(ChannelId, "Message", "Respones", 1, 50000, 500, 20000);
        }

        [Test]
        public virtual void Duplex_05_Send50_10Prallel()
        {
            SendMessageReceiveResponse(ChannelId, "Message", "Respones", 10, 50, 1000, 2000);
        }

        [Test]
        public virtual void Duplex_05a_SendBroadcastResponse_50_10Clients()
        {
            SendBroadcastResponseMessage(ChannelId, "broadcastMessage", 10, 50, 1000, 2000);
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
                Assert.AreEqual(aClient.OutputChannel.ResponseReceiverId, aClient.OutputChannel.ResponseReceiverId);

                // handling open connection on the service side.
                EneterTrace.Info("2");
                aService.WaitUntilResponseReceiversAreConnected(1, 1000);
                Assert.AreEqual(aClient.OutputChannel.ResponseReceiverId, aService.ConnectedResponseReceivers.First().ResponseReceiverId);

                aClient.OutputChannel.CloseConnection();
                Assert.IsFalse(aClient.OutputChannel.IsConnected);

                EneterTrace.Info("3");
                aClient.WaitUntilConnectionClosedIsNotified(1000);
                Assert.AreEqual(aClient.OutputChannel.ChannelId, aClient.NotifiedCloseConnection.ChannelId);
                Assert.AreEqual(aClient.OutputChannel.ResponseReceiverId, aClient.NotifiedCloseConnection.ResponseReceiverId);

                EneterTrace.Info("4");
                aService.WaitUntilAllResponseReceiversAreDisconnected(1000);
                Assert.AreEqual(aClient.OutputChannel.ResponseReceiverId, aService.DisconnectedResponseReceivers.First().ResponseReceiverId);
            }
            finally
            {
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
            aService.DoOnMessageReceived_SendResponse("Hi");

            try
            {
                aService.InputChannel.StartListening();

                // Client opens the connection.
                aClient.OutputChannel.OpenConnection();
                Assert.IsTrue(aClient.OutputChannel.IsConnected);

                aClient.WaitUntilConnectionOpenIsNotified(1000);
                Assert.AreEqual(aClient.OutputChannel.ChannelId, aClient.NotifiedOpenConnection.ChannelId);
                Assert.AreEqual(aClient.OutputChannel.ResponseReceiverId, aClient.OutputChannel.ResponseReceiverId);

                aService.WaitUntilResponseReceiversAreConnected(1, 1000);
                Assert.AreEqual(aClient.OutputChannel.ResponseReceiverId, aService.ConnectedResponseReceivers.First().ResponseReceiverId);

                // Client closes the connection.
                aClient.OutputChannel.CloseConnection();
                Assert.IsFalse(aClient.OutputChannel.IsConnected);

                aClient.WaitUntilConnectionClosedIsNotified(1000);
                Assert.AreEqual(aClient.OutputChannel.ChannelId, aClient.NotifiedCloseConnection.ChannelId);
                Assert.AreEqual(aClient.OutputChannel.ResponseReceiverId, aClient.NotifiedCloseConnection.ResponseReceiverId);

                aService.WaitUntilAllResponseReceiversAreDisconnected(1000);
                Assert.AreEqual(aClient.OutputChannel.ResponseReceiverId, aService.DisconnectedResponseReceivers.First().ResponseReceiverId);


                aClient.ClearTestResults();
                aService.ClearTestResults();


                // Client opens the connection 2nd time.
                aClient.OutputChannel.OpenConnection();
                Assert.IsTrue(aClient.OutputChannel.IsConnected);

                aClient.WaitUntilConnectionOpenIsNotified(1000);
                Assert.AreEqual(aClient.OutputChannel.ChannelId, aClient.NotifiedOpenConnection.ChannelId);
                Assert.AreEqual(aClient.OutputChannel.ResponseReceiverId, aClient.OutputChannel.ResponseReceiverId);

                aService.WaitUntilResponseReceiversAreConnected(1, 1000);
                Assert.AreEqual(aClient.OutputChannel.ResponseReceiverId, aService.ConnectedResponseReceivers.First().ResponseReceiverId);

                // Client sends the message.
                aClient.OutputChannel.SendMessage("Hello");

                aClient.WaitUntilResponseMessagesAreReceived(1, 1000);

                Assert.AreEqual("Hello", aService.ReceivedMessages.First().Message);
                Assert.AreEqual("Hi", aClient.ReceivedMessages.First().Message);
            }
            finally
            {
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
            IDuplexInputChannel aDuplexInputChannel = MessagingSystemFactory.CreateDuplexInputChannel(ChannelId);
            IDuplexOutputChannel aDuplexOutputChannel = MessagingSystemFactory.CreateDuplexOutputChannel(ChannelId);


            AutoResetEvent aConnectionReopenEvent = new AutoResetEvent(false);
            bool aConnectionStatusWhenConnectionClosed = true;
            bool isStopped = false;
            aDuplexOutputChannel.ConnectionClosed += (x, y) =>
            {
                if (!isStopped)
                {
                    aConnectionStatusWhenConnectionClosed = aDuplexOutputChannel.IsConnected;

                    Thread.Sleep(100);

                    // Try to open from the handler.
                    aDuplexOutputChannel.OpenConnection();

                    aConnectionReopenEvent.Set();

                    isStopped = true;
                }
            };

            try
            {
                aDuplexInputChannel.StartListening();

                aDuplexOutputChannel.OpenConnection();

                Thread.Sleep(500);

                aDuplexInputChannel.DisconnectResponseReceiver(aDuplexOutputChannel.ResponseReceiverId);

                aConnectionReopenEvent.WaitIfNotDebugging(1000);

                Assert.IsFalse(aConnectionStatusWhenConnectionClosed);
            }
            finally
            {
                aDuplexInputChannel.StopListening();
                aDuplexOutputChannel.CloseConnection();
            }
        }

        [Test]
        public virtual void Duplex_09_StopListening_SendMessage()
        {
            IDuplexInputChannel anInputChannel = MessagingSystemFactory.CreateDuplexInputChannel(ChannelId);

            AutoResetEvent aResponseReceiverConnectedEvent = new AutoResetEvent(false);
            anInputChannel.ResponseReceiverConnected += (x, y) =>
                {
                    aResponseReceiverConnectedEvent.Set();
                };
            
            IDuplexOutputChannel anOutputChannel = MessagingSystemFactory.CreateDuplexOutputChannel(ChannelId);

            bool isSomeException = false;

            try
            {
                // Duplex input channel starts to listen.
                anInputChannel.StartListening();

                // Duplex output channel conncets.
                anOutputChannel.OpenConnection();
                Assert.IsTrue(anOutputChannel.IsConnected);

                aResponseReceiverConnectedEvent.WaitIfNotDebugging(1000);

                // Duplex input channel stops to listen.
                anInputChannel.StopListening();
                Assert.IsFalse(anInputChannel.IsListening);

                Thread.Sleep(500);

                try
                {
                    // Try to send a message via the duplex output channel.
                    anOutputChannel.SendMessage("Message");
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
                anOutputChannel.CloseConnection();
                anInputChannel.StopListening();
            }
        }

        [Test]
        public virtual void Duplex_09_StopListening()
        {
            IDuplexInputChannel anInputChannel = MessagingSystemFactory.CreateDuplexInputChannel(ChannelId);

            AutoResetEvent anAllConnected = new AutoResetEvent(false);
            int aNumber = 0;
            anInputChannel.ResponseReceiverConnected += (x, y) =>
                {
                    ++aNumber;
                    if (aNumber == 3)
                    {
                        anAllConnected.Set();
                    }
                };

            AutoResetEvent anAllDisconnected = new AutoResetEvent(false);
            List<ResponseReceiverEventArgs> aDisconnects = new List<ResponseReceiverEventArgs>();
            anInputChannel.ResponseReceiverDisconnected += (x, y) =>
                {
                    lock (aDisconnects)
                    {
                        aDisconnects.Add(y);

                        if (aDisconnects.Count == 3)
                        {
                            anAllDisconnected.Set();
                        }
                    }
                };

            IDuplexOutputChannel anOutputChannel1 = MessagingSystemFactory.CreateDuplexOutputChannel(ChannelId);
            IDuplexOutputChannel anOutputChannel2 = MessagingSystemFactory.CreateDuplexOutputChannel(ChannelId);
            IDuplexOutputChannel anOutputChannel3 = MessagingSystemFactory.CreateDuplexOutputChannel(ChannelId);

            try
            {
                // Duplex input channel starts to listen.
                anInputChannel.StartListening();

                // Open connections
                anOutputChannel1.OpenConnection();
                anOutputChannel2.OpenConnection();
                anOutputChannel3.OpenConnection();
                Assert.IsTrue(anOutputChannel1.IsConnected);
                Assert.IsTrue(anOutputChannel2.IsConnected);
                Assert.IsTrue(anOutputChannel3.IsConnected);

                anAllConnected.WaitIfNotDebugging(1000);

                // Stop listening.
                anInputChannel.StopListening();
                Assert.IsFalse(anInputChannel.IsListening);

                anAllDisconnected.WaitIfNotDebugging(1000);

                // Wait if e.g. more that three disconnects are delivered then error.
                Thread.Sleep(200);

                Assert.AreEqual(3, aDisconnects.Count);
            }
            finally
            {
                anOutputChannel1.CloseConnection();
                anOutputChannel2.CloseConnection();
                anOutputChannel3.CloseConnection();
                anInputChannel.StopListening();
            }
        }

        [Test]
        public virtual void Duplex_10_DisconnectResponseReceiver()
        {
            AutoResetEvent aResponseReceiverConnectedEvent = new AutoResetEvent(false);
            AutoResetEvent aConnectionClosedEvent = new AutoResetEvent(false);
            AutoResetEvent aResponseReceiverDisconnectedEvent = new AutoResetEvent(false);

            bool aResponseReceiverConnectedFlag = false;
            bool aResponseReceiverDisconnectedFlag = false;

            bool aConnectionClosedReceivedInOutputChannelFlag = false;
            bool aResponseMessageReceivedFlag = false;

            // Create duplex input channel.
            IDuplexInputChannel aDuplexInputChannel = MessagingSystemFactory.CreateDuplexInputChannel(ChannelId);
            aDuplexInputChannel.ResponseReceiverConnected += (x, y) =>
                {
                    aResponseReceiverConnectedFlag = true;
                    aResponseReceiverConnectedEvent.Set();
                };
            aDuplexInputChannel.ResponseReceiverDisconnected += (x, y) =>
                {
                    aResponseReceiverDisconnectedFlag = true;
                    aResponseReceiverDisconnectedEvent.Set();
                };


            // Create duplex output channel.
            IDuplexOutputChannel aDuplexOutputChannel = MessagingSystemFactory.CreateDuplexOutputChannel(ChannelId);
            aDuplexOutputChannel.ResponseMessageReceived += (x, y) =>
                {
                    aResponseMessageReceivedFlag = true;
                };
            aDuplexOutputChannel.ConnectionClosed += (x, y) =>
                {
                    aConnectionClosedReceivedInOutputChannelFlag = true;
                    aConnectionClosedEvent.Set();
                };


            try
            {
                aDuplexInputChannel.StartListening();
                aDuplexOutputChannel.OpenConnection();

                // Wait until the connection is established.
                aResponseReceiverConnectedEvent.WaitIfNotDebugging(1000);

                // Disconnect response receiver from the duplex input channel.
                aDuplexInputChannel.DisconnectResponseReceiver(aDuplexOutputChannel.ResponseReceiverId);

                // Wait until the response receiver is disconnected.
                aConnectionClosedEvent.WaitIfNotDebugging(1000);

                Assert.IsTrue(aResponseReceiverConnectedFlag);
                Assert.IsTrue(aConnectionClosedReceivedInOutputChannelFlag);

                Assert.IsFalse(aResponseMessageReceivedFlag);

                // Disconnect response receiver shall generate the client disconnected event.
                aResponseReceiverDisconnectedEvent.WaitIfNotDebugging(1000);
                Assert.IsTrue(aResponseReceiverDisconnectedFlag);
            }
            finally
            {
                aDuplexOutputChannel.CloseConnection();
                aDuplexInputChannel.StopListening();
            }
        }

        [Test]
        public virtual void Duplex_11_CloseConnection()
        {
            IDuplexInputChannel aDuplexInputChannel = MessagingSystemFactory.CreateDuplexInputChannel(ChannelId);

            IDuplexOutputChannel aDuplexOutputChannel1 = MessagingSystemFactory.CreateDuplexOutputChannel(ChannelId);
            IDuplexOutputChannel aDuplexOutputChannel2 = MessagingSystemFactory.CreateDuplexOutputChannel(ChannelId);

            AutoResetEvent aResponseReceiverConnectedEvent = new AutoResetEvent(false);
            string aConnectedResponseReceiver = "";
            aDuplexInputChannel.ResponseReceiverConnected += (x, y) =>
                {
                    aConnectedResponseReceiver = y.ResponseReceiverId;
                    aResponseReceiverConnectedEvent.Set();
                };

            AutoResetEvent aResponseReceiverDisconnectedEvent = new AutoResetEvent(false);
            string aDisconnectedResponseReceiver = "";
            aDuplexInputChannel.ResponseReceiverDisconnected += (x, y) =>
                {
                    aDisconnectedResponseReceiver = y.ResponseReceiverId;
                    aResponseReceiverDisconnectedEvent.Set();
                };

            AutoResetEvent aMessageReceivedEvent = new AutoResetEvent(false);
            string aReceivedMessage = "";
            aDuplexInputChannel.MessageReceived += (x, y) =>
                {
                    aReceivedMessage = y.Message as string;
                    aMessageReceivedEvent.Set();
                };

            try
            {
                // Start listening.
                aDuplexInputChannel.StartListening();
                Assert.IsTrue(aDuplexInputChannel.IsListening);

                // Connect duplex output channel 1
                aDuplexOutputChannel1.OpenConnection();

                // Wait until connected.
                aResponseReceiverConnectedEvent.WaitIfNotDebugging(1000);
                Assert.AreEqual(aDuplexOutputChannel1.ResponseReceiverId, aConnectedResponseReceiver);
                Assert.IsTrue(aDuplexOutputChannel1.IsConnected);

                // Connect duplex output channel 2
                aDuplexOutputChannel2.OpenConnection();

                // Wait until connected.
                aResponseReceiverConnectedEvent.WaitIfNotDebugging(1000);
                Assert.AreEqual(aDuplexOutputChannel2.ResponseReceiverId, aConnectedResponseReceiver);
                Assert.IsTrue(aDuplexOutputChannel2.IsConnected);


                // Disconnect duplex output channel 1
                aDuplexOutputChannel1.CloseConnection();

                // Wait until disconnected
                aResponseReceiverDisconnectedEvent.WaitIfNotDebugging(1000);
                Thread.Sleep(100); // maybe other unwanted disconnection - give them some time.
                Assert.IsFalse(aDuplexOutputChannel1.IsConnected);
                Assert.IsTrue(aDuplexOutputChannel2.IsConnected);
                Assert.AreEqual(aDuplexOutputChannel1.ResponseReceiverId, aDisconnectedResponseReceiver);

                // The second duplex output channel must still work.
                aDuplexOutputChannel2.SendMessage("Message");

                aMessageReceivedEvent.WaitIfNotDebugging(1000);
                Assert.AreEqual("Message", aReceivedMessage);
            }
            finally
            {
                EneterTrace.Info("Finally section.");

                aDuplexOutputChannel1.CloseConnection();
                aDuplexOutputChannel2.CloseConnection();
                aDuplexInputChannel.StopListening();
            }
        }

        [Test]
        public virtual void Duplex_12_CloseFromConnectionOpened()
        {
            IDuplexInputChannel aDuplexInputChannel = MessagingSystemFactory.CreateDuplexInputChannel(ChannelId);
            IDuplexOutputChannel aDuplexOutputChannel = MessagingSystemFactory.CreateDuplexOutputChannel(ChannelId);

            bool isOpenedFlag = false;
            aDuplexOutputChannel.ConnectionOpened += (x, y) =>
                {
                    isOpenedFlag = aDuplexOutputChannel.IsConnected;

                    // Try to close the connection from this "open" event handler.
                    aDuplexOutputChannel.CloseConnection();
                };

            bool isClosedFlag = false;
            AutoResetEvent aConnectionClosedEvent = new AutoResetEvent(false);
            aDuplexOutputChannel.ConnectionClosed += (x, y) =>
                {
                    isClosedFlag = aDuplexOutputChannel.IsConnected == false;
                    aConnectionClosedEvent.Set();
                };

            try
            {
                //EneterTrace.StartProfiler();

                aDuplexInputChannel.StartListening();

                // Open connection - the event will try to close the connection.
                aDuplexOutputChannel.OpenConnection();

                aConnectionClosedEvent.WaitIfNotDebugging(1000);

                //EneterTrace.StopProfiler();

                Assert.IsTrue(isOpenedFlag);
                Assert.IsTrue(isClosedFlag);
            }
            finally
            {
                aDuplexInputChannel.StopListening();
                aDuplexOutputChannel.CloseConnection();
            }

            // To allow traces to finish.
            Thread.Sleep(100);
        }

        [Test]
        public virtual void Duplex_13_DisconnectFromResponseReceiverConnected()
        {
            IDuplexInputChannel aDuplexInputChannel = MessagingSystemFactory.CreateDuplexInputChannel(ChannelId);
            IDuplexOutputChannel aDuplexOutputChannel = MessagingSystemFactory.CreateDuplexOutputChannel(ChannelId);

            string aConnectedResponseReceiver = "";
            aDuplexInputChannel.ResponseReceiverConnected += (x, y) =>
                {
                    aConnectedResponseReceiver = y.ResponseReceiverId;

                    Thread.Sleep(30);

                    aDuplexInputChannel.DisconnectResponseReceiver(aConnectedResponseReceiver);
                };

            bool isDisconnectedFlag = false;
            AutoResetEvent aConnectionClosedEvent = new AutoResetEvent(false);
            aDuplexOutputChannel.ConnectionClosed += (x, y) =>
                {
                    isDisconnectedFlag = aDuplexOutputChannel.IsConnected == false;
                    aConnectionClosedEvent.Set();
                };

            try
            {
                aDuplexInputChannel.StartListening();

                // Open connection - the event will try to close the connection.
                aDuplexOutputChannel.OpenConnection();

                aConnectionClosedEvent.WaitIfNotDebugging(1000);

                Assert.AreEqual(aDuplexOutputChannel.ResponseReceiverId, aConnectedResponseReceiver);
                Assert.IsTrue(isDisconnectedFlag);
            }
            finally
            {
                aDuplexInputChannel.StopListening();
                aDuplexOutputChannel.CloseConnection();
            }
        }

        // Currently this does not work. Consider for future releases.
        //[Test]
        public virtual void Duplex_14_IdenticalResponseReceiverIds()
        {
            IDuplexInputChannel aDuplexInputChannel = MessagingSystemFactory.CreateDuplexInputChannel(ChannelId);
            IDuplexOutputChannel aDuplexOutputChannel1 = MessagingSystemFactory.CreateDuplexOutputChannel(ChannelId, "123");
            IDuplexOutputChannel aDuplexOutputChannel2 = MessagingSystemFactory.CreateDuplexOutputChannel(ChannelId, "123");

            List<string> aConnectedResponseReceivers = new List<string>();
            aDuplexInputChannel.ResponseReceiverConnected += (x, y) =>
            {
                lock (aConnectedResponseReceivers)
                {
                    aConnectedResponseReceivers.Add(y.ResponseReceiverId);
                }
            };

            ManualResetEvent aConnection2Closed = new ManualResetEvent(false);
            aDuplexOutputChannel2.ConnectionClosed += (x, y) =>
                {
                    aConnection2Closed.Set();
                };

            try
            {
                aDuplexInputChannel.StartListening();

                aDuplexOutputChannel1.OpenConnection();
                try
                {
                    aDuplexOutputChannel2.OpenConnection();
                }
                catch
                {
                    // If the 2nd connection throws an exception it is ok.
                }

                aConnection2Closed.WaitIfNotDebugging(1000);

                Assert.IsTrue(aDuplexOutputChannel1.IsConnected);
                Assert.IsFalse(aDuplexOutputChannel1.IsConnected);
            }
            finally
            {
                aDuplexInputChannel.StopListening();
                aDuplexOutputChannel1.CloseConnection();
                aDuplexOutputChannel2.CloseConnection();
            }
        }

        private void SendMessageReceiveResponse(string channelId, string message, string responseMessage,
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
                aService.WaitUntilResponseReceiversAreConnected(numberOfClients, openConnectionTimeout);
                Assert.AreEqual(aClientFarm.Clients.Count(), aService.ConnectedResponseReceivers.Count());
                foreach (ClientMock aClient in aClientFarm.Clients)
                {
                    Assert.IsTrue(aService.ConnectedResponseReceivers.Any(x => x.ResponseReceiverId == aClient.OutputChannel.ResponseReceiverId));
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

        private void SendBroadcastResponseMessage(string channelId, string broadcastMessage,
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
                aService.WaitUntilResponseReceiversAreConnected(numberOfClients, openConnectionTimeout);
                Assert.AreEqual(aClientFarm.Clients.Count(), aService.ConnectedResponseReceivers.Count());
                foreach (ClientMock aClient in aClientFarm.Clients)
                {
                    Assert.IsTrue(aService.ConnectedResponseReceivers.Any(x => x.ResponseReceiverId == aClient.OutputChannel.ResponseReceiverId));
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

        private string myMessage_10MB = RandomDataGenerator.GetString(10000000);
    }
}
