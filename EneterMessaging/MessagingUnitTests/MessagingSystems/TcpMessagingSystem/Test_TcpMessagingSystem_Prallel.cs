
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.Diagnostic;
using System.IO;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
using System.Net.Sockets;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using System.Threading;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.MessagingUnitTests.MessagingSystems.TcpMessagingSystem
{
    [TestFixture]
    public class Test_TcpMessagingSystem_Prallel : TcpMessagingSystemBase
    {
        [SetUp]
        public void Setup()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            //EneterTrace.TraceLog = new StreamWriter("d:/tracefile_tcp.txt");

            // Generate random number for the port.
            Random aRnd = new Random();
            int aPort = aRnd.Next(8000, 9000);

            MessagingSystemFactory = new TcpMessagingSystemFactory()
            {
                InputChannelThreading = new NoDispatching(),
                OutputChannelThreading = new NoDispatching()
            };

            ChannelId = "tcp://127.0.0.1:" + aPort + "/";
        }

    }
}