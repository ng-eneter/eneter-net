using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using NUnit.Framework;
using System.Threading;
using Eneter.Messaging.MessagingSystems.Composites.MonitoredMessagingComposit;
using Eneter.Messaging.DataProcessing.Serializing;

namespace Eneter.MessagingUnitTests.MessagingSystems.Composits.ConnectionMonitor
{
    public abstract class ReconnecterBaseTester
    {
        [Test]
        public void A01_Reconnect_DisconnectReceiver()
        {
            IDuplexInputChannel aDuplexInputChannel = MessagingSystemFactory.CreateDuplexInputChannel(ChannelId);
            IDuplexOutputChannel aDuplexOutputChannel = MessagingSystemFactory.CreateDuplexOutputChannel(ChannelId);

            Reconnecter aReconnecter = new Reconnecter(aDuplexOutputChannel, TimeSpan.FromMilliseconds(300), -1);

            AutoResetEvent aReconnectEvent = new AutoResetEvent(false);
            DuplexChannelEventArgs aReconnectEventArgs = null;
            aReconnecter.ConnectionOpened += (x, y) =>
                {
                    aReconnectEventArgs = y;
                    aReconnectEvent.Set();
                };

            AutoResetEvent aDisconnectEvent = new AutoResetEvent(false);
            DuplexChannelEventArgs aDisconnectEventArgs = null;
            aReconnecter.ConnectionClosed += (x, y) =>
                {
                    aDisconnectEventArgs = y;
                    aDisconnectEvent.Set();
                };


            AutoResetEvent aConnectionOpenedEvent = new AutoResetEvent(false);
            aDuplexOutputChannel.ConnectionOpened += (x, y) =>
                {
                    aConnectionOpenedEvent.Set();
                };

            AutoResetEvent aConnectionClosedEvent = new AutoResetEvent(false);
            aDuplexOutputChannel.ConnectionClosed += (x, y) =>
                {
                    aConnectionClosedEvent.Set();
                };

            try
            {
                aReconnecter.EnableReconnecting();

                // Start listening.
                aDuplexInputChannel.StartListening();
                aDuplexOutputChannel.OpenConnection();
                Thread.Sleep(2000);

                Assert.IsTrue(aDuplexOutputChannel.IsConnected);
                aConnectionOpenedEvent.WaitOne();

                // Disconnect the duplexoutput channel.
                aDuplexInputChannel.DisconnectResponseReceiver(aDuplexOutputChannel.ResponseReceiverId);

                // Wait until the disconnect is notified.
                aDisconnectEvent.WaitOne();
                aConnectionClosedEvent.WaitOne();

                Assert.AreEqual(aDuplexOutputChannel.ChannelId, aDisconnectEventArgs.ChannelId);
                Assert.AreEqual(aDuplexOutputChannel.ResponseReceiverId, aDisconnectEventArgs.ResponseReceiverId);

                // Wait until the reconnecter opens connection again.
                aReconnectEvent.WaitOne();
                Assert.AreEqual(aDuplexOutputChannel.ChannelId, aReconnectEventArgs.ChannelId);
                Assert.AreEqual(aDuplexOutputChannel.ResponseReceiverId, aReconnectEventArgs.ResponseReceiverId);

                Assert.IsTrue(aDuplexOutputChannel.IsConnected);

                aConnectionOpenedEvent.WaitOne();

            }
            finally
            {
                aReconnecter.DisableReconnecting();
                aDuplexOutputChannel.CloseConnection();
                aDuplexInputChannel.StopListening();
            }
        }

        [Test]
        public void A02_Reconnect_StopListening()
        {
            MonitoredMessagingFactory aConnectionMonitor = new MonitoredMessagingFactory(MessagingSystemFactory);

            IDuplexInputChannel aDuplexInputChannel = aConnectionMonitor.CreateDuplexInputChannel(ChannelId);
            IDuplexOutputChannel aDuplexOutputChannel = aConnectionMonitor.CreateDuplexOutputChannel(ChannelId);

            // Max 5 attempts to try to reconnect.
            Reconnecter aReconnecter = new Reconnecter(aDuplexOutputChannel, TimeSpan.FromMilliseconds(300), 5);

            AutoResetEvent aReconnectEvent = new AutoResetEvent(false);
            DuplexChannelEventArgs aReconnectEventArgs = null;
            aReconnecter.ConnectionOpened += (x, y) =>
            {
                aReconnectEventArgs = y;
                aReconnectEvent.Set();
            };

            AutoResetEvent aDisconnectEvent = new AutoResetEvent(false);
            DuplexChannelEventArgs aDisconnectEventArgs = null;
            aReconnecter.ConnectionClosed += (x, y) =>
            {
                aDisconnectEventArgs = y;
                aDisconnectEvent.Set();
            };

            AutoResetEvent aReconnecFailedEvent = new AutoResetEvent(false);
            DuplexChannelEventArgs aReconnectingFailedEventArgs = null;
            aReconnecter.ReconnectingFailed += (x, y) =>
                {
                    aReconnectingFailedEventArgs = y;
                    aReconnecFailedEvent.Set();
                };

            try
            {
                aReconnecter.EnableReconnecting();

                // Start listening.
                aDuplexInputChannel.StartListening();
                aDuplexOutputChannel.OpenConnection();
                Thread.Sleep(2000);

                Assert.IsTrue(aDuplexOutputChannel.IsConnected);
                Assert.IsNull(aReconnectingFailedEventArgs);
                Assert.IsNull(aDisconnectEventArgs);
                Assert.IsNull(aReconnectEventArgs);


                // Stop listening.
                // Note: The Reconnecter will try 5 times to reopen the connection, then it will notify, the reconnecion failed.
                aDuplexInputChannel.StopListening();

                // Wait until the disconnect is notified.
                aDisconnectEvent.WaitOne();
                Assert.AreEqual(aDuplexOutputChannel.ChannelId, aDisconnectEventArgs.ChannelId);
                Assert.AreEqual(aDuplexOutputChannel.ResponseReceiverId, aDisconnectEventArgs.ResponseReceiverId);
                Assert.IsFalse(aDuplexOutputChannel.IsConnected);
                Assert.IsNull(aReconnectEventArgs);

                // Wait until the reconnect fails after 5 failed reconnects.
                aReconnecFailedEvent.WaitOne();
                Assert.AreEqual(aDuplexOutputChannel.ChannelId, aReconnectingFailedEventArgs.ChannelId);
                Assert.AreEqual(aDuplexOutputChannel.ResponseReceiverId, aReconnectingFailedEventArgs.ResponseReceiverId);
                Assert.IsFalse(aDuplexOutputChannel.IsConnected);
                Assert.IsNull(aReconnectEventArgs);
            }
            finally
            {
                aDuplexOutputChannel.CloseConnection();
                aDuplexInputChannel.StopListening();
                aReconnecter.DisableReconnecting();
            }
        }

        protected string ChannelId { get; set; }
        protected IMessagingSystemFactory MessagingSystemFactory { get; set; }
    }
}
