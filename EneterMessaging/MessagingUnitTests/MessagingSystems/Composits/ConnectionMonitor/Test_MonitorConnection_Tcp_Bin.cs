﻿#if !SILVERLIGHT && !COMPACT_FRAMEWORK

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
using Eneter.Messaging.MessagingSystems.Composites.MonitoredMessagingComposit;
using System.Net.Sockets;

namespace Eneter.MessagingUnitTests.MessagingSystems.Composits.ConnectionMonitor
{
    [TestFixture]
    public class Test_MonitorConnection_Tcp_Bin : MonitorConnectionTesterBase
    {
        [SetUp]
        public void Setup()
        {
            ChannelId = "tcp://127.0.0.1:7080/";
            Serializer = new BinarySerializer();
            UnderlyingMessaging = new TcpMessagingSystemFactory();
            MessagingSystemFactory = new MonitoredMessagingFactory(UnderlyingMessaging, Serializer, TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(2000));
        }

    }
}

#endif