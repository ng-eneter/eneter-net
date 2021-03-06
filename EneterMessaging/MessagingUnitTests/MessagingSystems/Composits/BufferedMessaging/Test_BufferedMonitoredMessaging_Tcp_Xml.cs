﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.MessagingSystems.Composites;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.Diagnostic;
using System.IO;
using System.Threading;

namespace Eneter.MessagingUnitTests.MessagingSystems.Composits.BufferedMessaging
{
    [TestFixture]
    public class Test_BufferedMonitoredMessaging_Tcp_Xml : BufferedMessagingBaseTester
    {
        [SetUp]
        public void Setup()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            //EneterTrace.TraceLog = new StreamWriter("c:/tmp/tracefile.txt");
            //EneterTrace.StartProfiler();

            ChannelId = "tcp://127.0.0.1:6070/";

            IMessagingSystemFactory anUnderlyingMessaging = new TcpMessagingSystemFactory();
            TimeSpan aMaxOfflineTime = TimeSpan.FromMilliseconds(1000);
            MessagingSystem = new BufferedMonitoredMessagingFactory(anUnderlyingMessaging, aMaxOfflineTime, TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(100));
            ConnectionInterruptionFrequency = 100;
        }

        [TearDown]
        public void TearDown()
        {
            EneterTrace.StopProfiler();
            EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Short;
            if (EneterTrace.TraceLog is StreamWriter)
            {
                Thread.Sleep(500);
                TextWriter aTrace = EneterTrace.TraceLog;
                EneterTrace.TraceLog = null;
                aTrace.Close();
            }
        }
    }
}