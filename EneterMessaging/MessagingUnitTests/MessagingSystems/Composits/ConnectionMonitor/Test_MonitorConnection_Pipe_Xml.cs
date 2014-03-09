#if !SILVERLIGHT && !COMPACT_FRAMEWORK

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.Composites.MonitoredMessagingComposit;
using Eneter.Messaging.MessagingSystems.NamedPipeMessagingSystem;
using Eneter.Messaging.DataProcessing.Serializing;

namespace Eneter.MessagingUnitTests.MessagingSystems.Composits.ConnectionMonitor
{
    [TestFixture]
    public class Test_MonitorConnection_Pipe_Xml : MonitorConnectionTesterBase
    {
        [SetUp]
        public void Setup()
        {
            ChannelId = "net.pipe://127.0.0.1/ChannelId/";
            Serializer = new XmlStringSerializer();
            UnderlyingMessaging = new NamedPipeMessagingSystemFactory(10, 500);
            MessagingSystemFactory = new MonitoredMessagingFactory(UnderlyingMessaging, Serializer,
                TimeSpan.FromMilliseconds(1000),
                // e.g. if the communication is very intensive then it may take more time until the response is received.
                TimeSpan.FromMilliseconds(10000));
        }
    }
}


#endif