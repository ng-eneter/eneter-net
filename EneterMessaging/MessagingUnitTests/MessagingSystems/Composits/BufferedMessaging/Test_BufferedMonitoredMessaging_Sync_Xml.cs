using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.MessagingSystems.Composites.BufferedMessagingComposit;
using Eneter.Messaging.Diagnostic;
using System.IO;
using Eneter.Messaging.MessagingSystems.Composites;

namespace Eneter.MessagingUnitTests.MessagingSystems.Composits.BufferedMessaging
{
    [TestFixture]
    public class Test_BufferedMonitoredMessaging_Sync_Xml : BufferedMessagingBaseTester
    {
        [SetUp]
        public void Setup()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            //EneterTrace.TraceLog = new StreamWriter("d:/tracefile.txt");

            ChannelId = "Channel_1";
            IMessagingSystemFactory anUnderlyingMessaging = new SynchronousMessagingSystemFactory();
            TimeSpan aMaxOfflineTime = TimeSpan.FromMilliseconds(1000);
            TimeSpan aPingFrequency = TimeSpan.FromMilliseconds(50);
            TimeSpan aPingResponseTimeout = TimeSpan.FromMilliseconds(300);
            MessagingSystem = new BufferedMonitoredMessagingFactory(
                anUnderlyingMessaging,
                aMaxOfflineTime, aPingFrequency, aPingResponseTimeout);
            ConnectionInterruptionFrequency = 5;
        }
    }
}
