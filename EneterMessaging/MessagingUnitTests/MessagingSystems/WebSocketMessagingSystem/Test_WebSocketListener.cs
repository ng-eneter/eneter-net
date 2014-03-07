
#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using System.Text.RegularExpressions;
using Eneter.Messaging.MessagingSystems.WebSocketMessagingSystem;
using System.Threading;
using System.IO;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security;
using Eneter.Messaging.Diagnostic;
using System.Net;

namespace Eneter.MessagingUnitTests.MessagingSystems.WebSocketMessagingSystem
{
    [TestFixture]
    public class Test_WebSocketListener
    {
        [Test]
        public void Echo()
        {
            Uri anAddress = new Uri("ws://127.0.0.1:8087/");

            WebSocketListener aService = new WebSocketListener(anAddress);
            WebSocketClient aClient = new WebSocketClient(anAddress);

            try
            {
                AutoResetEvent aClientOpenedConnectionEvent = new AutoResetEvent(false);
                AutoResetEvent aResponseReceivedEvent = new AutoResetEvent(false);
                AutoResetEvent aClientContextReceivedPongEvent = new AutoResetEvent(false);
                AutoResetEvent aClientContextDisconnectedEvent = new AutoResetEvent(false);

                byte[] aReceivedBinMessage = null;
                string aReceivedTextMessage = "";

                bool aClientContextReceivedPong = false;
                bool aClientContextReceivedCloseConnection = false;
                bool aClientContextDisconnectedAfterClose = false;

                IPEndPoint aClientAddress = null;

                IWebSocketClientContext aClientContext = null;

                // Start listening.
                aService.StartListening(clientContext =>
                {
                    aClientContext = clientContext;

                    aClientAddress = aClientContext.ClientEndPoint;

                    clientContext.PongReceived += (x, y) =>
                    {
                        aClientContextReceivedPong = true;
                        aClientContextReceivedPongEvent.Set();
                    };

                    clientContext.ConnectionClosed += (x, y) =>
                    {
                        aClientContextReceivedCloseConnection = true;
                        aClientContextDisconnectedAfterClose = !clientContext.IsConnected;
                        aClientContextDisconnectedEvent.Set();
                    };

                    WebSocketMessage aWebSocketMessage;
                    while ((aWebSocketMessage = clientContext.ReceiveMessage()) != null)
                    {
                        if (!aWebSocketMessage.IsText)
                        {
                            aReceivedBinMessage = aWebSocketMessage.GetWholeMessage();

                            // echo
                            clientContext.SendMessage(aReceivedBinMessage);
                        }
                        else
                        {
                            aReceivedTextMessage = aWebSocketMessage.GetWholeTextMessage();

                            // echo
                            clientContext.SendMessage(aReceivedTextMessage);
                        }

                    }
                });

                // Client opens connection.
                bool aClientReceivedPong = false;
                bool aClientOpenedConnection = false;
                byte[] aReceivedBinResponse = null;
                string aReceivedTextResponse = "";

                aClient.ConnectionOpened += (x, y) =>
                {
                    aClientOpenedConnection = true;
                    aClientOpenedConnectionEvent.Set();
                };
                aClient.PongReceived += (x, y) => aClientReceivedPong = true;
                aClient.MessageReceived += (x, y) =>
                {
                    if (!y.IsText)
                    {
                        aReceivedBinResponse = y.GetWholeMessage();
                    }
                    else
                    {
                        aReceivedTextResponse = y.GetWholeTextMessage();
                    }

                    aResponseReceivedEvent.Set();
                };

                aClient.OpenConnection();
                aClientOpenedConnectionEvent.WaitOne();

                Assert.IsTrue(aClientOpenedConnection);
                Assert.AreEqual("127.0.0.1", aClientAddress.Address.ToString());
                Assert.AreNotEqual(8087, aClientAddress.Port);

                aClient.SendMessage("He", false);
                aClient.SendMessage("ll", false);
                aClient.SendPing();
                aClient.SendMessage("o", true);

                aResponseReceivedEvent.WaitOne();

                Assert.AreEqual("Hello", aReceivedTextMessage);
                Assert.AreEqual("Hello", aReceivedTextResponse);

                // Client sent ping so it had to receive back the pong.
                Assert.IsTrue(aClientReceivedPong);

                aClientContext.SendPing();

                aClientContextReceivedPongEvent.WaitOne();

                // Service sent ping via the client context - pong had to be received.
                Assert.IsTrue(aClientContextReceivedPong);


                byte[] aBinaryMessage = { 1, 2, 3, 100, 200 };
                aClient.SendMessage(new byte[] { 1, 2, 3 }, false);
                aClient.SendMessage(new byte[] { 100, 200 }, true);

                aResponseReceivedEvent.WaitOne();

                Assert.AreEqual(aBinaryMessage, aReceivedBinMessage);
                Assert.AreEqual(aBinaryMessage, aReceivedBinResponse);

                aClient.CloseConnection();

                aClientContextDisconnectedEvent.WaitOne();

                Assert.IsTrue(aClientContextReceivedCloseConnection);
                Assert.IsTrue(aClientContextDisconnectedAfterClose);
            }
            finally
            {
                aClient.CloseConnection();
                aService.StopListening();
            }

        }

        [Test]
        public void MultipleServicesOneTcpAddress()
        {
            Uri anAddress1 = new Uri("ws://127.0.0.1:8087/MyService1/");
            Uri anAddress2 = new Uri("ws://127.0.0.1:8087/MyService2/");

            WebSocketListener aService1 = new WebSocketListener(anAddress1);
            WebSocketClient aClient1 = new WebSocketClient(anAddress1);

            WebSocketListener aService2 = new WebSocketListener(anAddress2);
            WebSocketClient aClient2 = new WebSocketClient(anAddress2);

            try
            {
                AutoResetEvent aResponse1ReceivedEvent = new AutoResetEvent(false);
                AutoResetEvent aResponse2ReceivedEvent = new AutoResetEvent(false);

                string aReceivedTextMessage1 = "";
                string aReceivedTextMessage2 = "";

                // Start listening.
                aService1.StartListening(clientContext =>
                    {
                        WebSocketMessage aWebSocketMessage;
                        while ((aWebSocketMessage = clientContext.ReceiveMessage()) != null)
                        {
                            if (aWebSocketMessage.IsText)
                            {
                                string aMessage = aWebSocketMessage.GetWholeTextMessage();

                                // echo
                                clientContext.SendMessage(aMessage);
                            }
                        }
                    });

                aService2.StartListening(clientContext =>
                    {
                        WebSocketMessage aWebSocketMessage;
                        while ((aWebSocketMessage = clientContext.ReceiveMessage()) != null)
                        {
                            if (aWebSocketMessage.IsText)
                            {
                                string aMessage = aWebSocketMessage.GetWholeTextMessage();

                                // echo
                                clientContext.SendMessage(aMessage);
                            }
                        }
                    });

                // Client opens connection.
                aClient1.MessageReceived += (x, y) =>
                    {
                        if (y.IsText)
                        {
                            aReceivedTextMessage1 = y.GetWholeTextMessage();
                        }

                        aResponse1ReceivedEvent.Set();
                    };
                aClient2.MessageReceived += (x, y) =>
                    {
                        if (y.IsText)
                        {
                            aReceivedTextMessage2 = y.GetWholeTextMessage();
                        }

                        aResponse2ReceivedEvent.Set();
                    };


                aClient1.OpenConnection();
                aClient2.OpenConnection();

                aClient1.SendMessage("He", false);
                aClient1.SendMessage("ll", false);
                aClient2.SendMessage("Hello2");
                aClient1.SendPing();
                aClient1.SendMessage("o1", true);

                aResponse1ReceivedEvent.WaitOne();
                aResponse2ReceivedEvent.WaitOne();

                Assert.AreEqual("Hello1", aReceivedTextMessage1);
                Assert.AreEqual("Hello2", aReceivedTextMessage2);
            }
            finally
            {
                aClient1.CloseConnection();
                aClient2.CloseConnection();
                aService1.StopListening();
                aService2.StopListening();
            }
        }

        [Test]
        public void Query()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;

#if !COMPACT_FRAMEWORK
            Uri anAddress = new Uri("ws://localhost:8087/MyService/");
#else
            Uri anAddress = new Uri("ws://127.0.0.1:8087/MyService/");
#endif


            WebSocketListener aService = new WebSocketListener(anAddress);

            // Client will connect with the query.
            Uri aClientUri = new Uri(anAddress.ToString() + "?UserName=user1&Amount=1");
            WebSocketClient aClient = new WebSocketClient(aClientUri);

            try
            {
                AutoResetEvent aClientConnectedEvent = new AutoResetEvent(false);

                IWebSocketClientContext aClientContext = null;

                // Start listening.
                aService.StartListening(clientContext =>
                {
                    aClientContext = clientContext;

                    // Indicate the client is connected.
                    aClientConnectedEvent.Set();
                });

                aClient.OpenConnection();

                // Wait until the service accepted and opened the connection.
                Assert.IsTrue(aClientConnectedEvent.WaitOne(3000));
                //aClientConnectedEvent.WaitOne();

                // Check if the query is received.
                Assert.AreEqual("?UserName=user1&Amount=1", aClientContext.Uri.Query);
                Assert.AreEqual(aClientUri, aClientContext.Uri);

            }
            finally
            {
                aClient.CloseConnection();
                aService.StopListening();
            }

        }

        [Test]
        public void CustomHeaderFields()
        {
            Uri anAddress = new Uri("ws://127.0.0.1:8087/MyService/");
            WebSocketListener aService = new WebSocketListener(anAddress);

            // Client will connect with the query.
            WebSocketClient aClient = new WebSocketClient(anAddress);

            try
            {
                AutoResetEvent aClientConnectedEvent = new AutoResetEvent(false);

                IWebSocketClientContext aClientContext = null;

                // Start listening.
                aService.StartListening(clientContext =>
                {
                    aClientContext = clientContext;

                    // Indicate the client is connected.
                    aClientConnectedEvent.Set();
                });

                aClient.HeaderFields["My-Key1"] = "hello1";
                aClient.HeaderFields["My-Key2"] = "hello2";
                aClient.OpenConnection();

                // Wait until the service accepted and opened the connection.
                //Assert.IsTrue(aClientConnectedEvent.WaitOne(3000));
                aClientConnectedEvent.WaitOne();

                // Check if the query is received.
                Assert.AreEqual("hello1", aClientContext.HeaderFields["My-Key1"]);
                Assert.AreEqual("hello2", aClientContext.HeaderFields["My-Key2"]);
            }
            finally
            {
                aClient.CloseConnection();
                aService.StopListening();
            }

        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void TwoSameListeners()
        {
            Uri anAddress = new Uri("ws://127.0.0.1:8087/MyService/");
            WebSocketListener aService1 = new WebSocketListener(anAddress);

            WebSocketListener aService2 = new WebSocketListener(anAddress);

            try
            {
                // Start the first listener.
                aService1.StartListening(x => { });

                // Start the second listener to the same path -> exception is expected.
                aService2.StartListening(x => { });
            }
            finally
            {
                 aService1.StopListening();
                 aService2.StopListening();
            }

        }

        [Test]
        public void RestartListener()
        {
            Uri anAddress1 = new Uri("ws://127.0.0.1:8087/MyService1/");
            WebSocketListener aService1 = new WebSocketListener(anAddress1);

            Uri anAddress2 = new Uri("ws://127.0.0.1:8087/MyService2/");
            WebSocketListener aService2 = new WebSocketListener(anAddress2);

            try
            {
                // Start the first listener.
                aService1.StartListening(x => { });

                // Start the second listener.
                aService2.StartListening(x => { });

                aService2.StopListening();

                aService2.StartListening(x => { });
            }
            finally
            {
                aService1.StopListening();
                aService2.StopListening();
            }

        }
    }
}


#endif