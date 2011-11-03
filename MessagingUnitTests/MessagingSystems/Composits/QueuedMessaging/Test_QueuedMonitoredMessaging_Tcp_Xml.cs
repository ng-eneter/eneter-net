﻿#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.MessagingSystems.Composites;

namespace Eneter.MessagingUnitTests.MessagingSystems.Composits.QueuedMessaging
{
    [TestFixture]
    public class Test_QueuedMonitoredMessaging_Tcp_Xml : QueuedMessagingBaseTester
    {
        [SetUp]
        public void Setup()
        {
            ChannelId = "tcp://127.0.0.1:6070/";

            UnderlyingMessaging = new TcpMessagingSystemFactory();
            ISerializer aSerializer = new XmlStringSerializer();
            TimeSpan aMaxOfflineTime = TimeSpan.FromMilliseconds(1000);
            MessagingSystem = new BufferedMonitoredMessagingFactory(UnderlyingMessaging, aSerializer, aMaxOfflineTime, TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(100));
            ConnectionInterruptionFrequency = 100;
        }
    }
}


#endif