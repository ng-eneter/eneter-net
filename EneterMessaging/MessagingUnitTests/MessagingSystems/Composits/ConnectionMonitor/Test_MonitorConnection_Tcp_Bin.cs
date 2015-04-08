#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
using Eneter.Messaging.MessagingSystems.Composites.MonitoredMessagingComposit;
using System.Net.Sockets;
using Eneter.Messaging.Diagnostic;
using System.IO;

namespace Eneter.MessagingUnitTests.MessagingSystems.Composits.ConnectionMonitor
{
    [TestFixture]
    public class Test_MonitorConnection_Tcp_Bin : MonitorConnectionTesterBase
    {
        [SetUp]
        public void Setup()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            //EneterTrace.TraceLog = new StreamWriter("d:/tracefile.txt");

            ChannelId = "tcp://127.0.0.1:7080/";
            UnderlyingMessaging = new TcpMessagingSystemFactory();
            MessagingSystemFactory = new MonitoredMessagingFactory(UnderlyingMessaging,
                TimeSpan.FromMilliseconds(250),
                // e.g. if the communication is very intensive then it may take more time until the response is received.
                TimeSpan.FromMilliseconds(750)); 
        }

    }
}

#endif