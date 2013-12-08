using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;
using Eneter.Messaging.Nodes.BackupRouter;
using System.Threading;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
using Eneter.Messaging.Diagnostic;
using System.IO;

namespace Eneter.MessagingUnitTests.Nodes.BackupRuter
{
    [TestFixture]
    public class Test_BackupRouter
    {
        private class TService : IDisposable
        {
            public TService(IMessagingSystemFactory messaging, string channelId)
            {
                myInputChannel = messaging.CreateDuplexInputChannel(channelId);
                myInputChannel.ResponseReceiverConnected += OnResponseReceiverConnected;
                myInputChannel.ResponseReceiverDisconnected += OnResponseReceiverDisconnected;
                myInputChannel.MessageReceived += OnMessageReceived;
            }

            public void Dispose()
            {
                myInputChannel.StopListening();
                myInputChannel.ResponseReceiverConnected -= OnResponseReceiverConnected;
                myInputChannel.ResponseReceiverDisconnected -= OnResponseReceiverDisconnected;
                myInputChannel.MessageReceived -= OnMessageReceived;
            }

            void OnResponseReceiverConnected(object sender, ResponseReceiverEventArgs e)
            {
                lock (myConnectedClients)
                {
                    EneterTrace.Info("Client connection added " + e.ResponseReceiverId);
                    myConnectedClients.Add(e.ResponseReceiverId);
                }
            }

            void OnResponseReceiverDisconnected(object sender, ResponseReceiverEventArgs e)
            {
                lock (myConnectedClients)
                {
                    EneterTrace.Info("Client connection removed " + e.ResponseReceiverId);
                    myConnectedClients.Remove(e.ResponseReceiverId);
                }
            }

            void OnMessageReceived(object sender, DuplexChannelMessageEventArgs e)
            {
                lock (myReceivedMessages)
                {
                    myReceivedMessages.Add((string)e.Message);
                    myInputChannel.SendResponseMessage(e.ResponseReceiverId, "Response for " + (string)e.Message);
                }
            }

            public IDuplexInputChannel myInputChannel;
            public List<string> myConnectedClients = new List<string>();
            public List<string> myReceivedMessages = new List<string>();
        }

        private class TClient : IDisposable
        {
            public TClient(IMessagingSystemFactory messaging, string channelId)
            {
                myOutputChannel = messaging.CreateDuplexOutputChannel(channelId);
                myOutputChannel.ResponseMessageReceived += OnResponseMessageReceived;

            }
            
            public void Dispose()
            {
                myOutputChannel.CloseConnection();
            }

            void OnResponseMessageReceived(object sender, DuplexChannelMessageEventArgs e)
            {
                lock (myReceivedResponses)
                {
                    myReceivedResponses.Add((string)e.Message);
                    myResponseMessageReceived.Set();
                }
            }

            public AutoResetEvent myResponseMessageReceived = new AutoResetEvent(false);
            public IDuplexOutputChannel myOutputChannel;
            public List<string> myReceivedResponses = new List<string>();
        }

        [SetUp]
        public void Setup()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            //EneterTrace.TraceLog = new StreamWriter("d:/tracefile.txt");
        }

        [Test]
        public void SendMessageReceiveResponse()
        {
            IMessagingSystemFactory aServiceMessaging = new TcpMessagingSystemFactory();
            IMessagingSystemFactory aLocalMessaging = new SynchronousMessagingSystemFactory();

            // Create services.
            TService aService1 = new TService(aServiceMessaging, "tcp://127.0.0.1:8097/");
            TService aService2 = new TService(aServiceMessaging, "tcp://127.0.0.1:8098/");

            // Create the backup router.
            IBackupConnectionRouterFactory aBackupRouterFactory = new BackupConnectionRouterFactory(aServiceMessaging);
            IBackupConnectionRouter aBackupRouter = aBackupRouterFactory.CreateBackupConnectionRouter();
            List<RedirectEventArgs> aRedirections = new List<RedirectEventArgs>();
            AutoResetEvent aRedirectionEvent = new AutoResetEvent(false);
            aBackupRouter.ConnectionRedirected += (x, y) =>
                {
                    lock (aRedirections)
                    {
                        aRedirections.Add(y);
                        aRedirectionEvent.Set();
                    }
                };
            aBackupRouter.AddReceivers(new string[] { "tcp://127.0.0.1:8097/", "tcp://127.0.0.1:8098/" });
            Assert.AreEqual(2, aBackupRouter.AvailableReceivers.Count());

            IDuplexInputChannel aBackupRouterInputChannel = aLocalMessaging.CreateDuplexInputChannel("BackupRouter");
            aBackupRouter.AttachDuplexInputChannel(aBackupRouterInputChannel);

            // Create clients connected to the backup router.
            TClient aClient1 = new TClient(aLocalMessaging, "BackupRouter");
            TClient aClient2 = new TClient(aLocalMessaging, "BackupRouter");

            try
            {
                // Start both services.
                aService1.myInputChannel.StartListening();
                aService2.myInputChannel.StartListening();


                // Connect client 1.
                aClient1.myOutputChannel.OpenConnection();
                Thread.Sleep(300);
                Assert.AreEqual(1, aService1.myConnectedClients.Count);

                // Disconnect client 1.
                aClient1.myOutputChannel.CloseConnection();
                Thread.Sleep(300);
                Assert.AreEqual(0, aService1.myConnectedClients.Count);

                // Connect client 1 and 2.
                EneterTrace.Info("Client1 opens connection.");
                aClient1.myOutputChannel.OpenConnection();
                EneterTrace.Info("Client2 opens connection.");
                aClient2.myOutputChannel.OpenConnection();
                Thread.Sleep(300);
                Assert.AreEqual(2, aService1.myConnectedClients.Count);

                // Stop service 1.
                aService1.myInputChannel.StopListening();
                aService1.myConnectedClients.Clear();
                aRedirectionEvent.WaitOne();
                aRedirectionEvent.WaitOne();
                // Give some time until the service has connections.
                Thread.Sleep(500);
                Assert.AreEqual(2, aService2.myConnectedClients.Count);

                // Start service 1 again and stop Service 2.
                aService1.myInputChannel.StartListening();
                aService2.myInputChannel.StopListening();
                aService2.myConnectedClients.Clear();
                aRedirectionEvent.WaitOne();
                aRedirectionEvent.WaitOne();
                // Give some time until the service has connections.
                Thread.Sleep(300);
                Assert.AreEqual(2, aService1.myConnectedClients.Count);

                aService2.myInputChannel.StartListening();

                // Send the request message.
                aClient1.myOutputChannel.SendMessage("Hello from 1");
                aClient1.myResponseMessageReceived.WaitOne();
                aClient2.myOutputChannel.SendMessage("Hello from 2");
                aClient2.myResponseMessageReceived.WaitOne();
                Assert.AreEqual(2, aService1.myReceivedMessages.Count);
                Assert.AreEqual(0, aService2.myReceivedMessages.Count);
                Assert.AreEqual("Hello from 1", aService1.myReceivedMessages[0]);
                Assert.AreEqual("Hello from 2", aService1.myReceivedMessages[1]);
                Assert.AreEqual(1, aClient1.myReceivedResponses.Count);
                Assert.AreEqual(1, aClient2.myReceivedResponses.Count);
                Assert.AreEqual("Response for Hello from 1", aClient1.myReceivedResponses[0]);
                Assert.AreEqual("Response for Hello from 2", aClient2.myReceivedResponses[0]);
            }
            finally
            {
                aBackupRouter.RemoveAllReceivers();
                aClient1.Dispose();
                aClient2.Dispose();
                aService1.Dispose();
                aService2.Dispose();
            }
        }

        [Test]
        public void AllRedirectionsFailure()
        {
            IMessagingSystemFactory aServiceMessaging = new TcpMessagingSystemFactory();
            IMessagingSystemFactory aLocalMessaging = new SynchronousMessagingSystemFactory();

            // Create services.
            TService aService1 = new TService(aServiceMessaging, "tcp://127.0.0.1:8097/");

            // Create the backup router.
            IBackupConnectionRouterFactory aBackupRouterFactory = new BackupConnectionRouterFactory(aServiceMessaging);
            IBackupConnectionRouter aBackupRouter = aBackupRouterFactory.CreateBackupConnectionRouter();
            List<RedirectEventArgs> aRedirections = new List<RedirectEventArgs>();
            AutoResetEvent aFailedRedirectionsCompleted = new AutoResetEvent(false);
            int aFailedRedirections = 0;
            aBackupRouter.AllRedirectionsFailed += (x, y) =>
                {
                    ++aFailedRedirections;
                    if (aFailedRedirections == 2)
                    {
                        aFailedRedirectionsCompleted.Set();
                    }
                };
            // The second address is not listening.
            aBackupRouter.AddReceivers(new string[] { "tcp://127.0.0.1:8097/", "tcp://127.0.0.1:8098/" });

            IDuplexInputChannel aBackupRouterInputChannel = aLocalMessaging.CreateDuplexInputChannel("BackupRouter");
            aBackupRouter.AttachDuplexInputChannel(aBackupRouterInputChannel);

            // Create clients connected to the backup router.
            TClient aClient1 = new TClient(aLocalMessaging, "BackupRouter");
            TClient aClient2 = new TClient(aLocalMessaging, "BackupRouter");

            try
            {
                // Start only the 1st service.
                aService1.myInputChannel.StartListening();

                // Connect client 1 and 2.
                aClient1.myOutputChannel.OpenConnection();
                aClient2.myOutputChannel.OpenConnection();
                Thread.Sleep(5000);
                Assert.AreEqual(2, aService1.myConnectedClients.Count);

                // Stop service 1.
                aService1.myInputChannel.StopListening();
                aService1.myConnectedClients.Clear();
                // Give some time to notify failed redirections.
                aFailedRedirectionsCompleted.WaitOne();

                // Start service 1 again.
                aService1.myInputChannel.StartListening();

                // Send the request message. - the router reopen the connection when the message is sent.
                aClient1.myOutputChannel.SendMessage("Hello from 1");
                aClient1.myResponseMessageReceived.WaitOne();
                aClient2.myOutputChannel.SendMessage("Hello from 2");
                aClient2.myResponseMessageReceived.WaitOne();
                Assert.AreEqual(2, aService1.myReceivedMessages.Count);
                Assert.AreEqual("Hello from 1", aService1.myReceivedMessages[0]);
                Assert.AreEqual("Hello from 2", aService1.myReceivedMessages[1]);
                Assert.AreEqual(1, aClient1.myReceivedResponses.Count);
                Assert.AreEqual(1, aClient2.myReceivedResponses.Count);
                Assert.AreEqual("Response for Hello from 1", aClient1.myReceivedResponses[0]);
                Assert.AreEqual("Response for Hello from 2", aClient2.myReceivedResponses[0]);
            }
            finally
            {
                aBackupRouter.RemoveAllReceivers();
                aClient1.Dispose();
                aClient2.Dispose();
                aService1.Dispose();
            }
        }

        [Test]
        public void RemoveConnectedService()
        {
            IMessagingSystemFactory aServiceMessaging = new TcpMessagingSystemFactory();
            IMessagingSystemFactory aLocalMessaging = new SynchronousMessagingSystemFactory();

            // Create services.
            TService aService1 = new TService(aServiceMessaging, "tcp://127.0.0.1:8097/");
            TService aService2 = new TService(aServiceMessaging, "tcp://127.0.0.1:8098/");

            // Create the backup router.
            IBackupConnectionRouterFactory aBackupRouterFactory = new BackupConnectionRouterFactory(aServiceMessaging);
            IBackupConnectionRouter aBackupRouter = aBackupRouterFactory.CreateBackupConnectionRouter();
            List<RedirectEventArgs> aRedirections = new List<RedirectEventArgs>();
            AutoResetEvent aFailedRedirectionsCompleted = new AutoResetEvent(false);
            int aFailedRedirections = 0;
            aBackupRouter.AllRedirectionsFailed += (x, y) =>
            {
                ++aFailedRedirections;
                if (aFailedRedirections == 2)
                {
                    aFailedRedirectionsCompleted.Set();
                }
            };
            aBackupRouter.AddReceivers(new string[] { "tcp://127.0.0.1:8097/", "tcp://127.0.0.1:8098/" });

            IDuplexInputChannel aBackupRouterInputChannel = aLocalMessaging.CreateDuplexInputChannel("BackupRouter");
            aBackupRouter.AttachDuplexInputChannel(aBackupRouterInputChannel);

            // Create clients connected to the backup router.
            TClient aClient1 = new TClient(aLocalMessaging, "BackupRouter");
            TClient aClient2 = new TClient(aLocalMessaging, "BackupRouter");

            try
            {
                // Start both services.
                aService1.myInputChannel.StartListening();
                aService2.myInputChannel.StartListening();

                // Connect client 1 and 2.
                aClient1.myOutputChannel.OpenConnection();
                aClient2.myOutputChannel.OpenConnection();
                Thread.Sleep(300);
                Assert.AreEqual(2, aService1.myConnectedClients.Count);

                // Remove service1 from available services.
                aBackupRouter.RemoveReceiver(aService1.myInputChannel.ChannelId);
                // Give some time to redirect clients to service 2.
                Thread.Sleep(1000);

                // Send the request message. - the router reopen the connection when the message is sent.
                aClient1.myOutputChannel.SendMessage("Hello from 1");
                aClient1.myResponseMessageReceived.WaitOne();
                aClient2.myOutputChannel.SendMessage("Hello from 2");
                aClient2.myResponseMessageReceived.WaitOne();
                Assert.AreEqual(2, aService2.myReceivedMessages.Count);
                Assert.AreEqual("Hello from 1", aService2.myReceivedMessages[0]);
                Assert.AreEqual("Hello from 2", aService2.myReceivedMessages[1]);
                Assert.AreEqual(1, aClient1.myReceivedResponses.Count);
                Assert.AreEqual(1, aClient2.myReceivedResponses.Count);
                Assert.AreEqual("Response for Hello from 1", aClient1.myReceivedResponses[0]);
                Assert.AreEqual("Response for Hello from 2", aClient2.myReceivedResponses[0]);
            }
            finally
            {
                aBackupRouter.RemoveAllReceivers();
                aClient1.Dispose();
                aClient2.Dispose();
                aService1.Dispose();
                aService2.Dispose();
            }
        }
    }
}
