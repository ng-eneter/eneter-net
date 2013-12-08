using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using System.Threading;

namespace Eneter.MessagingUnitTests.MessagingSystems.TcpMessagingSystem
{
    public abstract class TcpMessagingSystemBase : BaseTester
    {
        [Test]
        public override void Duplex_14_DoNotAllowConnecting()
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

                aConnectionClosedEvent.WaitOne();

                Assert.IsNull(aConnectedResponseReceiver);

                Assert.AreEqual(aDuplexOutputChannel.ResponseReceiverId, aConnectionToken.ResponseReceiverId);
                Assert.IsFalse(aDuplexOutputChannel.IsConnected);
            }
            finally
            {
                aDuplexInputChannel.StopListening();
                aDuplexOutputChannel.CloseConnection();
            }
        }
    }
}
