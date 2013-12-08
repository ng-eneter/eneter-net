using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using System.Threading;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;

namespace Eneter.MessagingUnitTests.MessagingSystems.Composits.ConnectionMonitor
{
    public abstract class MonitorConnectionTesterBase : BaseTester
    {
        public override void Duplex_06_OpenCloseConnection()
        {
            // This test-case is not applicable, because the output channel sends the ping and that will reconnect the connection.
        }

        public override void Duplex_12_CloseFromConnectionOpened()
        {
            // This test-case is not applicable, because the output channel sends the ping and that will reconnect the connection.
        }

        [Test]
        public void B01_Pinging_StopListening()
        {
            IDuplexInputChannel aDuplexInputChannel = MessagingSystemFactory.CreateDuplexInputChannel(ChannelId);

            IDuplexOutputChannel aDuplexOutputChannel = MessagingSystemFactory.CreateDuplexOutputChannel(ChannelId);

            AutoResetEvent aDisconnectedEvent = new AutoResetEvent(false);

            bool aDisconnectedFlag = false;
            aDuplexOutputChannel.ConnectionClosed += (x, y) =>
                {
                    aDisconnectedFlag = true;
                    aDisconnectedEvent.Set();
                };

            try
            {
                // Start listening.
                aDuplexInputChannel.StartListening();

                // Start pinging and wait 5 seconds.
                aDuplexOutputChannel.OpenConnection();
                Thread.Sleep(5000);

                Assert.IsFalse(aDisconnectedFlag);

                // Stop listener, therefore the ping response will not come and the channel should indicate the disconnection.
                aDuplexInputChannel.StopListening();

                aDisconnectedEvent.WaitOne();

                Assert.IsTrue(aDisconnectedFlag);
                Assert.IsFalse(aDuplexOutputChannel.IsConnected);
            }
            finally
            {
                aDuplexOutputChannel.CloseConnection();
                aDuplexInputChannel.StopListening();
            }
        }

        [Test]
        public void B02_Pinging_DisconnectResponseReceiver()
        {
            IDuplexInputChannel aDuplexInputChannel = MessagingSystemFactory.CreateDuplexInputChannel(ChannelId);
            IDuplexOutputChannel aDuplexOutputChannel = MessagingSystemFactory.CreateDuplexOutputChannel(ChannelId);

            AutoResetEvent aDisconnectedEvent = new AutoResetEvent(false);

            bool aDisconnectedFlag = false;
            aDuplexOutputChannel.ConnectionClosed += (x, y) =>
            {
                aDisconnectedFlag = true;
                aDisconnectedEvent.Set();
            };

            try
            {
                // Start listening.
                aDuplexInputChannel.StartListening();

                // Allow some time for pinging.
                aDuplexOutputChannel.OpenConnection();
                Thread.Sleep(2000);

                Assert.IsFalse(aDisconnectedFlag);

                // Disconnect the duplex output channel.
                aDuplexInputChannel.DisconnectResponseReceiver(aDuplexOutputChannel.ResponseReceiverId);

                aDisconnectedEvent.WaitOne();

                Assert.IsTrue(aDisconnectedFlag);
                Assert.IsFalse(aDuplexOutputChannel.IsConnected);
            }
            finally
            {
                aDuplexOutputChannel.CloseConnection();
                aDuplexInputChannel.StopListening();
            }
        }

        [Test]
        public void B03_Pinging_NoResponseForPing()
        {
            // Create mock for the monitor duplex input channel.
            IDuplexInputChannel anUnderlyingDuplexInputChannel = UnderlyingMessaging.CreateDuplexInputChannel(ChannelId);
            Mock_MonitorDuplexInputChannel aDuplexInputChannel = new Mock_MonitorDuplexInputChannel(anUnderlyingDuplexInputChannel, Serializer);
            
            IDuplexOutputChannel aDuplexOutputChannel = MessagingSystemFactory.CreateDuplexOutputChannel(ChannelId);

            AutoResetEvent aDisconnectedEvent = new AutoResetEvent(false);

            bool aDisconnectedFlag = false;
            aDuplexOutputChannel.ConnectionClosed += (x, y) =>
            {
                aDisconnectedFlag = true;
                aDisconnectedEvent.Set();
            };

            try
            {
                // Start listening.
                aDuplexInputChannel.StartListening();

                // Allow some time for pinging.
                aDuplexOutputChannel.OpenConnection();
                Thread.Sleep(5000);

                EneterTrace.Info("B03_Pinging_NoResponseForPing() turned off responding for Ping.");
                Assert.IsFalse(aDisconnectedFlag);

                // Turn off the responding on pings.
                aDuplexInputChannel.ResponsePingFlag = false;

                aDisconnectedEvent.WaitOne();

                Assert.IsTrue(aDisconnectedFlag);
                Assert.IsFalse(aDuplexOutputChannel.IsConnected);
            }
            finally
            {
                aDuplexOutputChannel.CloseConnection();
                aDuplexInputChannel.StopListening();
            }
        }

        [Test]
        public void B04_Pinging_CloseConnection()
        {
            IDuplexInputChannel aDuplexInputChannel = MessagingSystemFactory.CreateDuplexInputChannel(ChannelId);

            IDuplexOutputChannel aDuplexOutputChannel = MessagingSystemFactory.CreateDuplexOutputChannel(ChannelId);

            AutoResetEvent aConnectionClosedEvent = new AutoResetEvent(false);
            string aClosedResponseReceiverId = "";
            aDuplexInputChannel.ResponseReceiverDisconnected += (x, y) =>
                {
                    aClosedResponseReceiverId = y.ResponseReceiverId;
                    aConnectionClosedEvent.Set();
                };

            try
            {
                // Start listening.
                aDuplexInputChannel.StartListening();

                // Allow some time for pinging.
                aDuplexOutputChannel.OpenConnection();
                Thread.Sleep(2000);

                Assert.AreEqual("", aClosedResponseReceiverId);

                // Close connection. Therefore the duplex input channel will not get any pings anymore.
                aDuplexOutputChannel.CloseConnection();

                aConnectionClosedEvent.WaitOne();

                Assert.AreEqual(aDuplexOutputChannel.ResponseReceiverId, aClosedResponseReceiverId);
                Assert.IsFalse(aDuplexOutputChannel.IsConnected);
            }
            finally
            {
                aDuplexOutputChannel.CloseConnection();
                aDuplexInputChannel.StopListening();
            }
        }

        protected ISerializer Serializer { get; set; }
        protected IMessagingSystemFactory UnderlyingMessaging { get; set; }
    }
}
