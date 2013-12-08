
#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.SharedMemoryMessagingSystem;
using Eneter.Messaging.Diagnostic;
using System.IO;
using System.Threading;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.MessagingUnitTests.MessagingSystems.SharedMemoryMessagingSystem
{
    [TestFixture]
    public class Test_SharedMemoryMessagingSystem : BaseTester
    {
        [SetUp]
        public void Setup()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            //EneterTrace.TraceLog = new StreamWriter("d:/tracefile.txt");

            MessagingSystemFactory = new SharedMemoryMessagingSystemFactory();

            ChannelId = "Channel1";
        }

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

                Exception anException = null;
                try
                {
                    aDuplexOutputChannel.OpenConnection();
                }
                catch (Exception err)
                {
                    anException = err;
                }

                aConnectionNotAllowedEvent.WaitOne();

                Assert.IsNotNull(anException);

                Assert.IsNull(aConnectedResponseReceiver);

                Assert.AreEqual(aDuplexOutputChannel.ResponseReceiverId, aConnectionToken.ResponseReceiverId);
            }
            finally
            {
                aDuplexInputChannel.StopListening();
                aDuplexOutputChannel.CloseConnection();
            }
        }
    }
}

#endif