using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using NUnit.Framework;
using System.Threading;
using Eneter.Messaging.Diagnostic;

namespace Eneter.MessagingUnitTests.MessagingSystems
{
    public abstract class MessagingSystemBaseTester
    {
        public MessagingSystemBaseTester()
        {
            ChannelId = "Channel1";
        }

        [Test]
        public virtual void A01_SendMessage()
        {
            SendMessageViaOutputChannel(ChannelId, "Message", 1, 5000);
        }

        [Test]
        public virtual void A02_SendMessages500()
        {
            SendMessageViaOutputChannel(ChannelId, "Message", 500, 5000);
        }

        [Ignore]
        [Test]
        public virtual void A03_SendMessage_10MB_1x()
        {
            // 10MB long message
            string aLongMessage = RandomDataGenerator.GetString(10000000);

            SendMessageViaOutputChannel(ChannelId, aLongMessage, 1, 10000);
        }

        [Ignore]
        [Test]
        public virtual void A04_SendMessage_10MB_100x()
        {
            // 10MB long message
            string aLongMessage = RandomDataGenerator.GetString(10000000);

            SendMessageViaOutputChannel(ChannelId, aLongMessage, 100, 60000);
        }

        [Test]
        public virtual void A05_SendMessageReceiveResponse()
        {
            SendMessageReceiveResponse(ChannelId, "Message", "Response", 1, 1000);
        }

        [Test]
        public virtual void A06_SendMessageReceiveResponse500()
        {
            SendMessageReceiveResponse(ChannelId, "Message", "Respones", 500, 1000);
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public virtual void A07_StopListening()
        {
            IOutputChannel anOutputChannel = MessagingSystemFactory.CreateOutputChannel(ChannelId);
            IInputChannel anInputChannel = MessagingSystemFactory.CreateInputChannel(ChannelId);

            ChannelMessageEventArgs aReceivedMessage = null;
            anInputChannel.MessageReceived += (x, y) =>
            {
                aReceivedMessage = y;
            };

            anInputChannel.StartListening();

            Thread.Sleep(100);

            anInputChannel.StopListening();

            // Send the message. Since the listening is stoped nothing should be delivered.
            anOutputChannel.SendMessage("Message");

            Thread.Sleep(500);

            Assert.IsNull(aReceivedMessage);
        }

        [Test]
        public void A08_MultithreadSendMessage()
        {
            IOutputChannel anOutputChannel = MessagingSystemFactory.CreateOutputChannel(ChannelId);
            IInputChannel anInputChannel = MessagingSystemFactory.CreateInputChannel(ChannelId);

            ManualResetEvent aMessagesSentEvent = new ManualResetEvent(false);

            // Observe the input channel
            List<string> aReceivedMessages = new List<string>();
            anInputChannel.MessageReceived += (x, y) =>
            {
                aReceivedMessages.Add((string)y.Message);

                Console.WriteLine(aReceivedMessages.Count.ToString() + " " + y.Message);

                if (aReceivedMessages.Count == 500)
                {
                    aMessagesSentEvent.Set();
                }
            };

            // Create 10 competing threads
            List<Thread> aThreads = new List<Thread>();
            for (int t = 0; t < 10; ++t)
            {
                Thread aThread = new Thread(() =>
                {
                    // Send 50 messages
                    for (int i = 0; i < 50; ++i)
                    {
                        Thread.Sleep(1); // To mix the order of threads. (othewise it would go thread by thread)
                        anOutputChannel.SendMessage(Thread.CurrentThread.ManagedThreadId.ToString());
                    }
                });
                aThreads.Add(aThread);
            }

            try
            {
                anInputChannel.StartListening();

                // Start sending from threads
                aThreads.ForEach(x => x.Start());

                // Wait for all threads.
                aThreads.ForEach(x => Assert.IsTrue(x.Join(20000)));

                Assert.IsTrue(aMessagesSentEvent.WaitOne(30000));
            }
            finally
            {
                anInputChannel.StopListening();
            }

            // Check
            Assert.AreEqual(500, aReceivedMessages.Count);
        }


        [Test]
        public virtual void A09_OpenCloseConnection()
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
                anOutputChannel.OpenConnection();

                aConnectionOpenedEvent.WaitOne();

                Assert.AreEqual(anOutputChannel.ChannelId, aConnectionOpenedEventArgs.ChannelId);
                Assert.AreEqual(anOutputChannel.ResponseReceiverId, aConnectionOpenedEventArgs.ResponseReceiverId);

                aResponseReceiverConnectedEvent.WaitOne();

                Assert.AreEqual(anOutputChannel.ResponseReceiverId, aConnectedReceiver);


                anOutputChannel.CloseConnection();

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
        public virtual void A10_OpenConnectionIfDuplexInputChannelNotStarted()
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
        public virtual void A11_DuplexInputChannelSuddenlyStopped()
        {
            IDuplexInputChannel anInputChannel = MessagingSystemFactory.CreateDuplexInputChannel(ChannelId);
            IDuplexOutputChannel anOutputChannel = MessagingSystemFactory.CreateDuplexOutputChannel(ChannelId);

            bool isSomeException = false;

            try
            {
                // Duplex input channel starts to listen.
                anInputChannel.StartListening();

                // Duplex output channel conncets.
                anOutputChannel.OpenConnection();
                Assert.IsTrue(anOutputChannel.IsConnected);

                Thread.Sleep(100);



                // Duplex input channel stops to listen.
                anInputChannel.StopListening();

                Assert.IsFalse(anInputChannel.IsListening);

                //Thread.Sleep(3000);

                // Try to send a message via the duplex output channel.
                anOutputChannel.SendMessage("Message");
            }
            catch
            {
                // Because the duplex input channel is not listening the sending must
                // fail with an exception. The type of the exception depends from the type of messaging system.
                isSomeException = true;
            }
            finally
            {
                anOutputChannel.CloseConnection();
            }

            Assert.IsTrue(isSomeException);
        }

        [Test]
        public virtual void A12_DuplexInputChannelDisconnectsResponseReceiver()
        {
            AutoResetEvent aResponseReceiverConnectedEvent = new AutoResetEvent(false);
            AutoResetEvent aConnectionClosedEvent = new AutoResetEvent(false);

            bool aResponseReceiverConnectedFlag = false;
            //bool aResponseReceiverDisconnectedFlag = false;

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
                    //aResponseReceiverDisconnectedFlag = true;
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
            }
            finally
            {
                aDuplexOutputChannel.CloseConnection();
                aDuplexInputChannel.StopListening();
            }

            Assert.IsTrue(aResponseReceiverConnectedFlag);
            Assert.IsTrue(aConnectionClosedReceivedInOutputChannelFlag);

            Assert.IsFalse(aResponseMessageReceivedFlag);
            
            // Note: When the duplex input channel disconnected the duplex output channel, the notification, that
            //       the duplex output channel was disconnected does not have to be invoked.
            //Assert.IsFalse(aResponseReceiverDisconnectedFlag);
        }

        [Test]
        public virtual void A13_DuplexOutputChannelClosesConnection()
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
                aDuplexOutputChannel1.CloseConnection();
                aDuplexOutputChannel2.CloseConnection();
                aDuplexInputChannel.StopListening();
            }
        }

        [Test]
        public virtual void A14_DuplexOutputChannelDisconnected_OpenFromCloseHandler()
        {
            IDuplexInputChannel aDuplexInputChannel = MessagingSystemFactory.CreateDuplexInputChannel(ChannelId);
            IDuplexOutputChannel aDuplexOutputChannel = MessagingSystemFactory.CreateDuplexOutputChannel(ChannelId);


            AutoResetEvent aConnectionReopenEvent = new AutoResetEvent(false);
            bool isConnected = true;
            bool isStopped = false;
            aDuplexOutputChannel.ConnectionClosed += (x, y) =>
                {
                    if (!isStopped)
                    {
                        isConnected = aDuplexOutputChannel.IsConnected;

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
            }
            finally
            {
                aDuplexInputChannel.StopListening();
                aDuplexOutputChannel.CloseConnection();
            }

            Assert.IsFalse(isConnected);
        }

        [Test]
        public virtual void A15_DuplexOutputChannelConnected_CloseFromOpenHandler()
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
        public virtual void A16_DuplexOutputChannelConnectionOpened_DisconnectFromOpenHandler()
        {
            IDuplexInputChannel aDuplexInputChannel = MessagingSystemFactory.CreateDuplexInputChannel(ChannelId);
            IDuplexOutputChannel aDuplexOutputChannel = MessagingSystemFactory.CreateDuplexOutputChannel(ChannelId);

            string aConnectedResponseReceiver = "";
            aDuplexInputChannel.ResponseReceiverConnected += (x, y) =>
                {
                    aConnectedResponseReceiver = y.ResponseReceiverId;

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

        /// <summary>
        /// Universal message sender
        /// </summary>
        /// <param name="message"></param>
        /// <param name="numberOfTimes"></param>
        private void SendMessageViaOutputChannel(string channelId, object message, int numberOfTimes, int timeOutForMessageProcessing)
        {
            IOutputChannel anOutputChannel = MessagingSystemFactory.CreateOutputChannel(channelId);
            IInputChannel anInputChannel = MessagingSystemFactory.CreateInputChannel(channelId);

            ManualResetEvent aMessagesSentEvent = new ManualResetEvent(false);

            object aLock = new object();
            int aNumberOfReceivedMessages = 0;
            int aNumberOfFailures = 0;
            anInputChannel.MessageReceived += (x, y) =>
                {
                    // Some messaging system can have a parallel access therefore we must ensure
                    // that results are put to the list synchronously.
                    lock (aLock)
                    {
                        ++aNumberOfReceivedMessages;

                        if (channelId != y.ChannelId || (string)message != (string)y.Message)
                        {
                            ++aNumberOfFailures;
                        }

                        // Release helper thread when all messages are received.
                        if (aNumberOfReceivedMessages == numberOfTimes)
                        {
                            aMessagesSentEvent.Set();
                        }
                    }
                };

            try
            {
                anInputChannel.StartListening();

                // Send messages
                for (int i = 0; i < numberOfTimes; ++i)
                {
                    anOutputChannel.SendMessage(message);
                }

                EneterTrace.Info("Send messages to '" + ChannelId + "' completed - waiting while they are processed.");

                // Wait until all messages are processed.
                Assert.IsTrue(aMessagesSentEvent.WaitOne(timeOutForMessageProcessing));
                //Assert.IsTrue(aMessagesSentEvent.WaitOne());

                EneterTrace.Info("Waiting for processing of messages on '" + ChannelId + "' completed.");
            }
            finally
            {
                anInputChannel.StopListening();
            }

            Assert.AreEqual(0, aNumberOfFailures);
        }

        private void SendMessageReceiveResponse(string channelId, object message, object resonseMessage, int numberOfTimes, int timeOutForMessageProcessing)
        {
            IDuplexOutputChannel anOutputChannel = MessagingSystemFactory.CreateDuplexOutputChannel(channelId);
            IDuplexInputChannel anInputChannel = MessagingSystemFactory.CreateDuplexInputChannel(channelId);

            AutoResetEvent aMessagesSentEvent = new AutoResetEvent(false);

            object aMessageReceiverLock = new object();
            int aNumberOfReceivedMessages = 0;
            int aNumberOfFailedMessages = 0;
            anInputChannel.MessageReceived += (x, y) =>
                {
                    // Some messaging system can have a parallel access therefore we must ensure
                    // that results are put to the list synchronously.
                    lock (aMessageReceiverLock)
                    {
                        ++aNumberOfReceivedMessages;

                        if (channelId != y.ChannelId || (string)message != (string)y.Message)
                        {
                            ++aNumberOfFailedMessages;
                        }
                        else
                        {
                            // everything is ok -> send the response
                            anInputChannel.SendResponseMessage(y.ResponseReceiverId, resonseMessage);
                        }
                    }
                };

            object aResponseReceiverLock = new object();
            int aNumberOfReceivedResponses = 0;
            int aNumberOfFailedResponses = 0;
            anOutputChannel.ResponseMessageReceived += (x, y) =>
                {
                    lock (aResponseReceiverLock)
                    {
                        ++aNumberOfReceivedResponses;
                        if ((string)resonseMessage != (string)y.Message)
                        {
                            ++aNumberOfFailedResponses;
                        }

                        // Release helper thread when all messages are received.
                        if (aNumberOfReceivedResponses == numberOfTimes)
                        {
                            aMessagesSentEvent.Set();
                        }
                    }
                };

            try
            {
                // Input channel starts listening
                anInputChannel.StartListening();

                // Output channel connects in order to be able to receivce response messages.
                anOutputChannel.OpenConnection();

                // Send messages
                for (int i = 0; i < numberOfTimes; ++i)
                {
                    anOutputChannel.SendMessage(message);
                }

                EneterTrace.Info("Send messages to '" + ChannelId + "' completed - waiting while they are processed.");

                // Wait until all messages are processed.
                //Assert.IsTrue(aMessagesSentEvent.WaitOne(timeOutForMessageProcessing));
                Assert.IsTrue(aMessagesSentEvent.WaitOne());

                EneterTrace.Info("Waiting for processing of messages on '" + ChannelId + "' completed.");
            }
            finally
            {
                try
                {
                    anOutputChannel.CloseConnection();
                }
                finally
                {
                    anInputChannel.StopListening();
                }
            }

            Assert.AreEqual(0, aNumberOfFailedMessages, "There are failed messages.");
            Assert.AreEqual(0, aNumberOfFailedResponses, "There are failed response messages.");
            Assert.AreEqual(numberOfTimes, aNumberOfReceivedMessages, "Number of sent messages differs from number of received.");
            Assert.AreEqual(numberOfTimes, aNumberOfReceivedResponses, "Number of received responses differs from number of sent responses.");
        }


        protected IMessagingSystemFactory MessagingSystemFactory { get; set; }

        protected string ChannelId { get; set; }
    }
}
