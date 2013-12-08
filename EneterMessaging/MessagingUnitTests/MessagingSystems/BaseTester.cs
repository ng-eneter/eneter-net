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
        private class TDuplexClient
        {
            public TDuplexClient(IMessagingSystemFactory messaging, string channelId, string expectedResponseMessage,
                int expectedNumberOfResponseMessages)
            {
                OutputChannel = messaging.CreateDuplexOutputChannel(channelId);
                OutputChannel.ResponseMessageReceived += OnResponseMessageReceived;

                ConnectionOpenEvent = new ManualResetEvent(false);
                ResponsesReceivedEvent = new ManualResetEvent(false);
                myExpectedResponseMessage = expectedResponseMessage;
                myExpectedNumberOfResponses = expectedNumberOfResponseMessages;
            }

            public void OpenConnection()
            {
                OutputChannel.OpenConnection();
                ConnectionOpenEvent.Set();
            }

            private void OnResponseMessageReceived(object sender, DuplexChannelMessageEventArgs e)
            {
                lock (myResponseReceiverLock)
                {
                    ++NumberOfReceivedResponses;

                    //EneterTrace.Info("Received Responses: " + NumberOfReceivedResponses);

                    if (myExpectedResponseMessage != (string)e.Message)
                    {
                        ++NumberOfFailedResponses;
                    }

                    // Release helper thread when all messages are received.
                    if (NumberOfReceivedResponses == myExpectedNumberOfResponses)
                    {
                        ResponsesReceivedEvent.Set();
                    }
                }
            }

            public int NumberOfReceivedResponses { get; private set; }
            public int NumberOfFailedResponses { get; private set; }
            public IDuplexOutputChannel OutputChannel { get; private set; }

            public ManualResetEvent ConnectionOpenEvent { get; private set; }
            public ManualResetEvent ResponsesReceivedEvent { get; private set; }

            private int myExpectedNumberOfResponses;
            private string myExpectedResponseMessage;
            private object myResponseReceiverLock = new object();
        }

        private class TDuplexService
        {
            public TDuplexService(IMessagingSystemFactory messaging, string channelId, string expextedMessage,
                int expextedNumberOfMessages,
                string responseMessage)
            {
                InputChannel = messaging.CreateDuplexInputChannel(channelId);
                InputChannel.MessageReceived += OnMessageReceived;

                MessagesReceivedEvent = new ManualResetEvent(false);

                myExpectedMessage = expextedMessage;
                myExpectedNumberOfMessages = expextedNumberOfMessages;
                myResponseMessage = responseMessage;
            }

            private void OnMessageReceived(object sender, DuplexChannelMessageEventArgs e)
            {
                // Some messaging system can have a parallel access therefore we must ensure
                // that results are put to the list synchronously.
                lock (myLock)
                {
                    ++NumberOfReceivedMessages;

                    //EneterTrace.Info("Received Messages: " + NumberOfReceivedMessages);

                    if (NumberOfReceivedMessages == myExpectedNumberOfMessages)
                    {
                        MessagesReceivedEvent.Set();
                    }

                    if (InputChannel.ChannelId != e.ChannelId || myExpectedMessage != (string)e.Message)
                    {
                        ++NumberOfFailedMessages;
                    }
                    else
                    {
                        // everything is ok -> send the response
                        InputChannel.SendResponseMessage(e.ResponseReceiverId, myResponseMessage);
                    }
                }
            }


            public IDuplexInputChannel InputChannel { get; private set; }
            public ManualResetEvent MessagesReceivedEvent { get; private set; }

            public int NumberOfReceivedMessages { get; private set; }
            public int NumberOfFailedMessages { get; private set; }

            private int myExpectedNumberOfMessages;
            private string myExpectedMessage;
            private string myResponseMessage;
            private object myLock = new object();
        }



        public BaseTester()
        {
            ChannelId = "Channel1";
        }

        [Test]
        public virtual void Duplex_01_Send1()
        {
            SendMessageReceiveResponse(ChannelId, "Message", "Response", 1, 1);
        }

        [Test]
        public virtual void Duplex_02_Send500()
        {
            SendMessageReceiveResponse(ChannelId, "Message", "Respones", 1, 500);
        }

        [Test]
        public virtual void Duplex_03_Send100_10MB()
        {
            SendMessageReceiveResponse(ChannelId, myMessage_10MB, myMessage_10MB, 1, 100);
        }

        [Test]
        public virtual void Duplex_04_Send50000()
        {
            SendMessageReceiveResponse(ChannelId, "Message", "Respones", 1, 50000);
        }

        [Test]
        public virtual void Duplex_05_Send50_10Prallel()
        {
            SendMessageReceiveResponse(ChannelId, "Message", "Respones", 10, 50);
        }


        [Test]
        public virtual void Duplex_06_OpenCloseConnection()
        {
            IDuplexInputChannel anInputChannel = MessagingSystemFactory.CreateDuplexInputChannel(ChannelId);
            IDuplexOutputChannel anOutputChannel = MessagingSystemFactory.CreateDuplexOutputChannel(ChannelId);

            AutoResetEvent aResponseReceiverConnectedEvent = new AutoResetEvent(false);
            string aConnectedReceiver = "";
            anInputChannel.ResponseReceiverConnected += (x, y) =>
                {
                    aConnectedReceiver = y.ResponseReceiverId;

                    aResponseReceiverConnectedEvent.Set();
                };

            AutoResetEvent aResponseReceiverDisconnectedEvent = new AutoResetEvent(false);
            string aDisconnectedReceiver = "";
            anInputChannel.ResponseReceiverDisconnected += (x, y) =>
                {
                    aDisconnectedReceiver = y.ResponseReceiverId;

                    aResponseReceiverDisconnectedEvent.Set();
                };



            AutoResetEvent aConnectionOpenedEvent = new AutoResetEvent(false);
            DuplexChannelEventArgs aConnectionOpenedEventArgs = null;
            anOutputChannel.ConnectionOpened += (x, y) =>
                {
                    aConnectionOpenedEventArgs = y;
                    aConnectionOpenedEvent.Set();
                };

            AutoResetEvent aConnectionClosedEvent = new AutoResetEvent(false);
            DuplexChannelEventArgs aConnectionClosedEventArgs = null;
            anOutputChannel.ConnectionClosed += (x, y) =>
                {
                    aConnectionClosedEventArgs = y;
                    aConnectionClosedEvent.Set();
                };


            try
            {
                anInputChannel.StartListening();

                // Open the connection.
                anOutputChannel.OpenConnection();
                Assert.IsTrue(anOutputChannel.IsConnected);

                // handling open connection on the client side.
                aConnectionOpenedEvent.WaitOne();
                Assert.AreEqual(anOutputChannel.ChannelId, aConnectionOpenedEventArgs.ChannelId);
                Assert.AreEqual(anOutputChannel.ResponseReceiverId, aConnectionOpenedEventArgs.ResponseReceiverId);

                // handling open connection on the service side.
                aResponseReceiverConnectedEvent.WaitOne();
                Assert.AreEqual(anOutputChannel.ResponseReceiverId, aConnectedReceiver);


                anOutputChannel.CloseConnection();
                Assert.IsFalse(anOutputChannel.IsConnected);

                aConnectionClosedEvent.WaitOne();
                Assert.AreEqual(anOutputChannel.ChannelId, aConnectionClosedEventArgs.ChannelId);
                Assert.AreEqual(anOutputChannel.ResponseReceiverId, aConnectionClosedEventArgs.ResponseReceiverId);

                aResponseReceiverDisconnectedEvent.WaitOne();
                Assert.AreEqual(anOutputChannel.ResponseReceiverId, aDisconnectedReceiver);
            }
            catch
            {
                anOutputChannel.CloseConnection();
                throw;
            }
            finally
            {
                anInputChannel.StopListening();
            }


            Assert.AreNotEqual("", aConnectedReceiver);
            Assert.AreNotEqual("", aDisconnectedReceiver);
            Assert.AreEqual(aConnectedReceiver, aDisconnectedReceiver);

        }

        [Test]
        public virtual void Duplex_06_OpenCloseOpenSend()
        {
            IDuplexInputChannel anInputChannel = MessagingSystemFactory.CreateDuplexInputChannel(ChannelId);
            IDuplexOutputChannel anOutputChannel = MessagingSystemFactory.CreateDuplexOutputChannel(ChannelId);

            AutoResetEvent aResponseReceiverConnectedEvent = new AutoResetEvent(false);
            string aConnectedReceiver = "";
            anInputChannel.ResponseReceiverConnected += (x, y) =>
            {
                aConnectedReceiver = y.ResponseReceiverId;

                aResponseReceiverConnectedEvent.Set();
            };

            AutoResetEvent aResponseReceiverDisconnectedEvent = new AutoResetEvent(false);
            string aDisconnectedReceiver = "";
            anInputChannel.ResponseReceiverDisconnected += (x, y) =>
            {
                aDisconnectedReceiver = y.ResponseReceiverId;

                aResponseReceiverDisconnectedEvent.Set();
            };

            AutoResetEvent aRequestMessageReceivedEvent = new AutoResetEvent(false);
            anInputChannel.MessageReceived += (x, y) =>
            {
                aRequestMessageReceivedEvent.Set();

                // send back the response.
                anInputChannel.SendResponseMessage(y.ResponseReceiverId, "Hi");
            };


            AutoResetEvent aConnectionOpenedEvent = new AutoResetEvent(false);
            DuplexChannelEventArgs aConnectionOpenedEventArgs = null;
            anOutputChannel.ConnectionOpened += (x, y) =>
            {
                aConnectionOpenedEventArgs = y;
                aConnectionOpenedEvent.Set();
            };

            AutoResetEvent aConnectionClosedEvent = new AutoResetEvent(false);
            DuplexChannelEventArgs aConnectionClosedEventArgs = null;
            anOutputChannel.ConnectionClosed += (x, y) =>
            {
                aConnectionClosedEventArgs = y;
                aConnectionClosedEvent.Set();
            };

            AutoResetEvent aResponseReceivedEvent = new AutoResetEvent(false);
            anOutputChannel.ResponseMessageReceived += (x, y) =>
                {
                    aResponseReceivedEvent.Set();
                };


            try
            {
                anInputChannel.StartListening();

                // Client opens the connection.
                anOutputChannel.OpenConnection();
                Assert.IsTrue(anOutputChannel.IsConnected);

                // handling open connection on the client side.
                aConnectionOpenedEvent.WaitOne();
                Assert.AreEqual(anOutputChannel.ChannelId, aConnectionOpenedEventArgs.ChannelId);
                Assert.AreEqual(anOutputChannel.ResponseReceiverId, aConnectionOpenedEventArgs.ResponseReceiverId);

                // handling open connection on the service side.
                aResponseReceiverConnectedEvent.WaitOne();
                Assert.AreEqual(anOutputChannel.ResponseReceiverId, aConnectedReceiver);


                // Client closes the connection.
                anOutputChannel.CloseConnection();
                Assert.IsFalse(anOutputChannel.IsConnected);

                aConnectionClosedEvent.WaitOne();
                Assert.AreEqual(anOutputChannel.ChannelId, aConnectionClosedEventArgs.ChannelId);
                Assert.AreEqual(anOutputChannel.ResponseReceiverId, aConnectionClosedEventArgs.ResponseReceiverId);

                aResponseReceiverDisconnectedEvent.WaitOne();
                Assert.AreEqual(anOutputChannel.ResponseReceiverId, aDisconnectedReceiver);


                // Client opens the connection.
                anOutputChannel.OpenConnection();
                Assert.IsTrue(anOutputChannel.IsConnected);

                // handling open connection on the client side.
                aConnectionOpenedEvent.WaitOne();
                Assert.AreEqual(anOutputChannel.ChannelId, aConnectionOpenedEventArgs.ChannelId);
                Assert.AreEqual(anOutputChannel.ResponseReceiverId, aConnectionOpenedEventArgs.ResponseReceiverId);

                // handling open connection on the service side.
                aResponseReceiverConnectedEvent.WaitOne();
                Assert.AreEqual(anOutputChannel.ResponseReceiverId, aConnectedReceiver);


                // Client sends a message.
                anOutputChannel.SendMessage("Hello");

                aRequestMessageReceivedEvent.WaitOne();
                aResponseReceivedEvent.WaitOne();
            }
            finally
            {
                anOutputChannel.CloseConnection();
                anInputChannel.StopListening();
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

                aConnectionReopenEvent.WaitOne();

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

                aResponseReceiverConnectedEvent.WaitOne();

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
        public virtual void Duplex_09_StopListeing()
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

                anAllConnected.WaitOne();

                // Stop listening.
                anInputChannel.StopListening();
                Assert.IsFalse(anInputChannel.IsListening);

                anAllDisconnected.WaitOne();

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
                aResponseReceiverConnectedEvent.WaitOne();

                // Disconnect response receiver from the duplex input channel.
                aDuplexInputChannel.DisconnectResponseReceiver(aDuplexOutputChannel.ResponseReceiverId);

                // Wait until the response receiver is disconnected.
                aConnectionClosedEvent.WaitOne();

                Assert.IsTrue(aResponseReceiverConnectedFlag);
                Assert.IsTrue(aConnectionClosedReceivedInOutputChannelFlag);

                Assert.IsFalse(aResponseMessageReceivedFlag);

                // Disconnect response receiver shall generate the client disconnected event.
                aResponseReceiverDisconnectedEvent.WaitOne();
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
                aResponseReceiverConnectedEvent.WaitOne();
                Assert.AreEqual(aDuplexOutputChannel1.ResponseReceiverId, aConnectedResponseReceiver);
                Assert.IsTrue(aDuplexOutputChannel1.IsConnected);

                // Connect duplex output channel 2
                aDuplexOutputChannel2.OpenConnection();

                // Wait until connected.
                aResponseReceiverConnectedEvent.WaitOne();
                Assert.AreEqual(aDuplexOutputChannel2.ResponseReceiverId, aConnectedResponseReceiver);
                Assert.IsTrue(aDuplexOutputChannel2.IsConnected);


                // Disconnect duplex output channel 1
                aDuplexOutputChannel1.CloseConnection();

                // Wait until disconnected
                aResponseReceiverDisconnectedEvent.WaitOne();
                Thread.Sleep(100); // maybe other unwanted disconnection - give them some time.
                Assert.IsFalse(aDuplexOutputChannel1.IsConnected);
                Assert.IsTrue(aDuplexOutputChannel2.IsConnected);
                Assert.AreEqual(aDuplexOutputChannel1.ResponseReceiverId, aDisconnectedResponseReceiver);

                // The second duplex output channel must still work.
                aDuplexOutputChannel2.SendMessage("Message");

                aMessageReceivedEvent.WaitOne();
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
                aDuplexInputChannel.StartListening();

                // Open connection - the event will try to close the connection.
                aDuplexOutputChannel.OpenConnection();

                aConnectionClosedEvent.WaitOne();

                Assert.IsTrue(isOpenedFlag);
                Assert.IsTrue(isClosedFlag);
            }
            finally
            {
                aDuplexInputChannel.StopListening();
                aDuplexOutputChannel.CloseConnection();
            }

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

                aConnectionClosedEvent.WaitOne();

                Assert.AreEqual(aDuplexOutputChannel.ResponseReceiverId, aConnectedResponseReceiver);
                Assert.IsTrue(isDisconnectedFlag);
            }
            finally
            {
                aDuplexInputChannel.StopListening();
                aDuplexOutputChannel.CloseConnection();
            }
        }

        [Test]
        public virtual void Duplex_14_DoNotAllowConnecting()
        {
            IDuplexInputChannel aDuplexInputChannel = MessagingSystemFactory.CreateDuplexInputChannel(ChannelId);
            IDuplexOutputChannel aDuplexOutputChannel = MessagingSystemFactory.CreateDuplexOutputChannel(ChannelId);

            AutoResetEvent aConnectionNotAllowedEvent = new AutoResetEvent(false);
            ConnectionTokenEventArgs aConnectionToken = null;
            aDuplexInputChannel.ResponseReceiverConnecting += (x, y) =>
                {
                    aConnectionToken = y;

                    // Indicate the connection is not allowed.
                    y.IsConnectionAllowed = false;
                    aConnectionNotAllowedEvent.Set();
                };

            ResponseReceiverEventArgs aConnectedResponseReceiver = null;
            aDuplexInputChannel.ResponseReceiverConnected += (x, y) =>
                {
                    aConnectedResponseReceiver = y;
                };

            AutoResetEvent aConnectionClosedEvent = new AutoResetEvent(false);
            aDuplexOutputChannel.ConnectionClosed += (x, y) =>
                {
                    aConnectionClosedEvent.Set();
                };

            try
            {
                aDuplexInputChannel.StartListening();

                // Open connection - the event will try to close the connection.
                aDuplexOutputChannel.OpenConnection();

                aConnectionNotAllowedEvent.WaitOne();

                // Note: Not all messagings are really physically connted so the duplex output channel
                //       does not have to receive it was disconneced.
                //       e.g. TCP gets this notification.
                //aConnectionClosedEvent.WaitOne();

                Assert.IsNull(aConnectedResponseReceiver);

                Assert.AreEqual(aDuplexOutputChannel.ResponseReceiverId, aConnectionToken.ResponseReceiverId);
                //Assert.IsFalse(aDuplexOutputChannel.IsConnected);
            }
            finally
            {
                aDuplexInputChannel.StopListening();
                aDuplexOutputChannel.CloseConnection();
            }
        }


        private void SendMessageReceiveResponse(string channelId, string message, string responseMessage,
                                                int numberOfClients, int numberOfMessages)
        {
            // Create number of desired clients.
            TDuplexClient[] aClients = new TDuplexClient[numberOfClients];
            for (int i = 0; i < numberOfClients; ++i)
            {
                aClients[i] = new TDuplexClient(MessagingSystemFactory, channelId, responseMessage, numberOfMessages);
            }

            // Create service.
            TDuplexService aService = new TDuplexService(MessagingSystemFactory, channelId, message, numberOfMessages * numberOfClients, responseMessage);

            try
            {
                // Service starts listening.
                aService.InputChannel.StartListening();

                // Clients open connection in parallel.
                foreach (TDuplexClient aClient in aClients)
                {
                    TDuplexClient aC = aClient;
                    WaitCallback aW = x =>
                        {
                            aC.OpenConnection();
                        };
                    ThreadPool.QueueUserWorkItem(aW);

                    Thread.Sleep(2);
                }

                // Wait until connections are open.
                foreach (TDuplexClient aClient in aClients)
                {
                    aClient.ConnectionOpenEvent.WaitOne();
                    //Assert.IsTrue(aClient.ConnectionOpenEvent.WaitOne(100000));
                }

                //EneterTrace.StartProfiler();

                Stopwatch aStopWatch = new Stopwatch();
                aStopWatch.Start();

                // Clients send messages in parallel.
                int idx = 0;
                foreach (TDuplexClient aClient in aClients)
                {
                    int aIdx = idx;
                    TDuplexClient aC = aClient;
                    WaitCallback aW = x =>
                    {
                        //EneterTrace.Info("Client idx = " + aIdx);

                        for (int j = 0; j < numberOfMessages; ++j)
                        {
                            // Send messages.
                            aC.OutputChannel.SendMessage(message);
                        }
                    };
                    ThreadPool.QueueUserWorkItem(aW);

                    Thread.Sleep(2);

                    ++idx;
                }

                // Wait until all messages are processed.
                foreach (TDuplexClient aClient in aClients)
                {
                    //Assert.IsTrue(aClient.ResponsesReceivedEvent.WaitOne(timeOutForMessageProcessing));
                    Assert.IsTrue(aClient.ResponsesReceivedEvent.WaitOne());
                }

                aStopWatch.Stop();
                Console.WriteLine("Send messages to '" + ChannelId + "' completed. Elapsed time = " + aStopWatch.Elapsed);

                //EneterTrace.StopProfiler();
            }
            finally
            {
                try
                {
                    foreach (TDuplexClient aClient in aClients)
                    {
                        aClient.OutputChannel.CloseConnection();
                    }
                }
                finally
                {
                    aService.InputChannel.StopListening();
                }
            }

            foreach (TDuplexClient aClient in aClients)
            {
                Assert.AreEqual(0, aClient.NumberOfFailedResponses, "There are failed response messages.");
                Assert.AreEqual(numberOfMessages, aClient.NumberOfReceivedResponses, "Number of received responses differs from number of sent responses.");
            }

            Assert.AreEqual(0, aService.NumberOfFailedMessages, "There are failed messages.");
            Assert.AreEqual(numberOfMessages * numberOfClients, aService.NumberOfReceivedMessages, "Number of sent messages differs from number of received.");
            
        }


        protected IMessagingSystemFactory MessagingSystemFactory { get; set; }

        protected string ChannelId { get; set; }

        private string myMessage_10MB = RandomDataGenerator.GetString(10000000);
    }
}
