

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
using System.Net.Sockets;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using System.Threading;
using Eneter.Messaging.Diagnostic;
using System.IO;

namespace Eneter.MessagingUnitTests.MessagingSystems.TcpMessagingSystem
{
    [TestFixture]
    public class Test_TcpMessagingSystem_Synchronous : TcpMessagingSystemBase
    {
        [SetUp]
        public void Setup()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            //EneterTrace.TraceLog = new StreamWriter("d:/tracefile.txt");
            //EneterTrace.StartProfiler();

            // Generate random number for the port.
            string aPort = RandomPortGenerator.Generate();

            MessagingSystemFactory = new TcpMessagingSystemFactory();
            //ChannelId = "tcp://127.0.0.1:" + aPort + "/";
            ChannelId = "tcp://[::1]:" + aPort + "/";
        }

        //[TearDown]
        //public void Clean()
        //{
        //    EneterTrace.StopProfiler();
        //}

        [Test]
        public void ConnectionTimeout()
        {
            IMessagingSystemFactory aMessaging = new TcpMessagingSystemFactory()
            {
                ConnectTimeout = TimeSpan.FromMilliseconds(1000)
            };

            // Nobody is listening on this address.
            IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("tcp://109.74.151.135:8045/");

            ManualResetEvent aConnectionCompleted = new ManualResetEvent(false);

            try
            {
                // Start opening in another thread to be able to measure
                // if the timeout occured with the specified time.
                Exception anException = null;
                ThreadPool.QueueUserWorkItem(x =>
                {
                    try
                    {
                        anOutputChannel.OpenConnection();
                    }
                    catch (Exception err)
                    {
                        anException = err;

                    }
                    aConnectionCompleted.Set();
                });

                if (aConnectionCompleted.WaitOne(1500))
                {
                }

                Assert.AreEqual(typeof(TimeoutException), anException);
            }
            finally
            {
                anOutputChannel.CloseConnection();
            }
        }

        [Test]
        public void ClientReceiveTimeout()
        {
            IMessagingSystemFactory aMessaging = new TcpMessagingSystemFactory()
            {
                ReceiveTimeout = TimeSpan.FromMilliseconds(1000)
            };
            IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("tcp://127.0.0.1:8046/");
            IDuplexInputChannel anInputChannel = aMessaging.CreateDuplexInputChannel("tcp://127.0.0.1:8046/");

            try
            {
                ManualResetEvent aConnectionClosed = new ManualResetEvent(false);
                anOutputChannel.ConnectionClosed += (x, y) =>
                {
                    EneterTrace.Info("Connection closed.");
                    aConnectionClosed.Set();
                };

                anInputChannel.StartListening();
                anOutputChannel.OpenConnection();

                EneterTrace.Info("Connection opened.");

                // According to set receive timeout the client should get disconnected within 1 second.
                //aConnectionClosed.WaitOne();
                Assert.IsTrue(aConnectionClosed.WaitOne(3000));
            }
            finally
            {
                anOutputChannel.CloseConnection();
                anInputChannel.StopListening();
            }
        }

        [Test]
        public void ServiceReceiveTimeout()
        {
            IMessagingSystemFactory aMessaging = new TcpMessagingSystemFactory()
            {
                ReceiveTimeout = TimeSpan.FromMilliseconds(1000)
            };
            IDuplexOutputChannel anOutputChannel = aMessaging.CreateDuplexOutputChannel("tcp://127.0.0.1:8046/");
            IDuplexInputChannel anInputChannel = aMessaging.CreateDuplexInputChannel("tcp://127.0.0.1:8046/");

            try
            {
                ManualResetEvent aConnectionClosed = new ManualResetEvent(false);
                anInputChannel.ResponseReceiverDisconnected += (x, y) =>
                {
                    EneterTrace.Info("Response Receiver Disconnected: " + y.ResponseReceiverId);
                };
                anOutputChannel.ConnectionClosed += (x, y) =>
                {
                    EneterTrace.Info("Connection closed.");
                    aConnectionClosed.Set();
                };


                anInputChannel.StartListening();
                anOutputChannel.OpenConnection();

                EneterTrace.Info("Connection opened: " + anOutputChannel.ResponseReceiverId);

                // According to set receive timeout the client should get disconnected within 1 second.
                //aConnectionClosed.WaitOne();
                Assert.IsTrue(aConnectionClosed.WaitOne(3000));
            }
            finally
            {
                anOutputChannel.CloseConnection();
                anInputChannel.StopListening();
            }
        }

        [Test]
        public void PortAvailability()
        {
            IDuplexInputChannel anInputChannel1 = MessagingSystemFactory.CreateDuplexInputChannel("tcp://[::1]:8044/");
            IDuplexInputChannel anInputChannel2 = MessagingSystemFactory.CreateDuplexInputChannel("tcp://127.0.0.1:8044/");

            try
            {
                anInputChannel1.StartListening();
                anInputChannel2.StartListening();

                Console.WriteLine("Available IP addresses:");
                string[] anAvailableIpAddresses = TcpMessagingSystemFactory.GetAvailableIpAddresses();
                foreach (string anIpAddress in anAvailableIpAddresses)
                {
                    Console.WriteLine(anIpAddress);
                }

                bool aResult = TcpMessagingSystemFactory.IsPortAvailableForTcpListening("tcp://[::1]:8044/");
                Assert.IsFalse(aResult);

                aResult = TcpMessagingSystemFactory.IsPortAvailableForTcpListening("tcp://0.0.0.0:8044/");
                Assert.IsTrue(aResult);
            }
            finally
            {
                anInputChannel1.StopListening();
                anInputChannel2.StopListening();
            }
        }


        [Test]
        public void MaxAmountOfConnections()
        {
            IMessagingSystemFactory aMessaging = new TcpMessagingSystemFactory()
            {
                MaxAmountOfConnections = 2
            };
            IDuplexOutputChannel anOutputChannel1 = aMessaging.CreateDuplexOutputChannel("tcp://127.0.0.1:8049/");
            IDuplexOutputChannel anOutputChannel2 = aMessaging.CreateDuplexOutputChannel("tcp://127.0.0.1:8049/");
            IDuplexOutputChannel anOutputChannel3 = aMessaging.CreateDuplexOutputChannel("tcp://127.0.0.1:8049/");
            IDuplexInputChannel anInputChannel = aMessaging.CreateDuplexInputChannel("tcp://127.0.0.1:8049/");

            try
            {
                ManualResetEvent aConnectionClosed = new ManualResetEvent(false);
                anOutputChannel3.ConnectionClosed += (x, y) =>
                {
                    EneterTrace.Info("Connection closed.");
                    aConnectionClosed.Set();
                };


                anInputChannel.StartListening();
                anOutputChannel1.OpenConnection();
                anOutputChannel2.OpenConnection();
                anOutputChannel3.OpenConnection();

                if (!aConnectionClosed.WaitOne(1000))
                {
                    Assert.Fail("Third connection was not closed.");
                }

                Assert.IsTrue(anOutputChannel1.IsConnected);
                Assert.IsTrue(anOutputChannel2.IsConnected);
                Assert.IsFalse(anOutputChannel3.IsConnected);
            }
            finally
            {
                anOutputChannel1.CloseConnection();
                anOutputChannel2.CloseConnection();
                anOutputChannel3.CloseConnection();
                anInputChannel.StopListening();
            }
        }
    }
}