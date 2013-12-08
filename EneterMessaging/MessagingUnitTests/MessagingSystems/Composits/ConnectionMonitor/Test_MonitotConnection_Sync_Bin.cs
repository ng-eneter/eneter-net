#if !SILVERLIGHT && !COMPACT_FRAMEWORK

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;
using Eneter.Messaging.MessagingSystems.Composites.MonitoredMessagingComposit;
using Eneter.Messaging.DataProcessing.Serializing;

namespace Eneter.MessagingUnitTests.MessagingSystems.Composits.ConnectionMonitor
{
    [TestFixture]
    public class Test_MonitotConnection_Sync_Bin : MonitorConnectionTesterBase
    {
        [SetUp]
        public void Setup()
        {
            ChannelId = "ChannelId";
            Serializer = new BinarySerializer();
            UnderlyingMessaging = new SynchronousMessagingSystemFactory();
            MessagingSystemFactory = new MonitoredMessagingFactory(UnderlyingMessaging, Serializer, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200));
        }
    }
}

#endif