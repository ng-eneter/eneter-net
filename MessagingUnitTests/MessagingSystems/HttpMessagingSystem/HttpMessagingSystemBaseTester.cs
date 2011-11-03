#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.HttpMessagingSystem;
using System.Threading;
using Eneter.Messaging.Diagnostic;

namespace Eneter.MessagingUnitTests.MessagingSystems.HttpMessagingSystem
{
    public abstract class HttpMessagingSystemBaseTester : MessagingSystemBaseTester
    {
        private class TConnectionEvent
        {
            public TConnectionEvent(DateTime time, string receiverId)
            {
                Time = time;
                ReceiverId = receiverId;
            }

            public DateTime Time { get; private set; }
            public string ReceiverId { get; private set; }
        }

        [Test]
        public void B01_InactivityTimeout()
        {
            // Set the pulling frequency time (duplex output channel pulls for responses) higher
            // than inactivity timeout in the duplex input channel.
            // Therefore the timeout should occur before the pulling - this is how the
            // inactivity is simulated in this test.
            IMessagingSystemFactory aMessagingSystem = new HttpMessagingSystemFactory(3000, 2000);

            IDuplexOutputChannel anOutputChannel1 = aMessagingSystem.CreateDuplexOutputChannel(ChannelId);
            IDuplexOutputChannel anOutputChannel2 = aMessagingSystem.CreateDuplexOutputChannel(ChannelId);

            IDuplexInputChannel anInputChannel = aMessagingSystem.CreateDuplexInputChannel(ChannelId);

            AutoResetEvent aConncetionEvent = new AutoResetEvent(false);
            AutoResetEvent aDisconncetionEvent = new AutoResetEvent(false);

            List<TConnectionEvent> aConnections = new List<TConnectionEvent>();
            anInputChannel.ResponseReceiverConnected += (x, y) =>
                {
                    aConnections.Add(new TConnectionEvent(DateTime.Now, y.ResponseReceiverId));
                    aConncetionEvent.Set();
                };

            List<TConnectionEvent> aDisconnections = new List<TConnectionEvent>();
            anInputChannel.ResponseReceiverDisconnected += (x, y) =>
                {
                    aDisconnections.Add(new TConnectionEvent(DateTime.Now, y.ResponseReceiverId));
                    aDisconncetionEvent.Set();
                };

            try
            {
                anInputChannel.StartListening();
                Assert.IsTrue(anInputChannel.IsListening);

                // Create the 1st connection.
                anOutputChannel1.OpenConnection();
                Assert.IsTrue(anOutputChannel1.IsConnected);
                aConncetionEvent.WaitOne();
                aConncetionEvent.Reset();
                Assert.AreEqual(1, aConnections.Count);
                Assert.IsFalse(string.IsNullOrEmpty(aConnections[0].ReceiverId));

                Thread.Sleep(1000);

                // Create the 2nd connection.
                anOutputChannel2.OpenConnection();
                Assert.IsTrue(anOutputChannel2.IsConnected);
                aConncetionEvent.WaitOne();
                Assert.AreEqual(2, aConnections.Count);
                Assert.IsFalse(string.IsNullOrEmpty(aConnections[1].ReceiverId));

                // Wait for the 1st disconnection
                aDisconncetionEvent.WaitOne();
                aDisconncetionEvent.Reset();

                Assert.AreEqual(1, aDisconnections.Count);
                Assert.AreEqual(aConnections[0].ReceiverId, aDisconnections[0].ReceiverId);
                Assert.IsTrue(aDisconnections[0].Time - aConnections[0].Time > TimeSpan.FromMilliseconds(2000));

                // Wait for the 2nd disconnection
                aDisconncetionEvent.WaitOne();

                Assert.AreEqual(2, aDisconnections.Count);
                Assert.AreEqual(aConnections[1].ReceiverId, aDisconnections[1].ReceiverId);
                Assert.IsTrue(aDisconnections[1].Time - aConnections[1].Time > TimeSpan.FromMilliseconds(2000));
            }
            finally
            {
                anOutputChannel1.CloseConnection();
                anOutputChannel2.CloseConnection();
                anInputChannel.StopListening();
            }
        }
    }
}

#endif