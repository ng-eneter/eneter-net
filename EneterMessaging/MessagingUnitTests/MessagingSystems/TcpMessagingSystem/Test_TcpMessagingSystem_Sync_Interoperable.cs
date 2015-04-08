#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;

namespace Eneter.MessagingUnitTests.MessagingSystems.TcpMessagingSystem
{
    [TestFixture]
    public class Test_TcpMessagingSystem_Sync_Interoperable : TcpMessagingSystemBase
    {
        [SetUp]
        public void Setup()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            //EneterTrace.TraceLog = new StreamWriter("d:/tracefile.txt");
            //EneterTrace.StartProfiler();

            // Generate random number for the port.
            string aPort = RandomPortGenerator.Generate();

            MessagingSystemFactory = new TcpMessagingSystemFactory(new EasyProtocolFormatter());
            //ChannelId = "tcp://127.0.0.1:" + aPort + "/";
            ChannelId = "tcp://[::1]:" + aPort + "/";

            this.CompareResponseReceiverId = false;
            this.myRequestMessage = new byte[] { (byte)'M', (byte)'E', (byte)'S', (byte)'S', (byte)'A', (byte)'G', (byte)'E' };
            this.myResponseMessage = new byte[] { (byte)'R', (byte)'E', (byte)'S', (byte)'P', (byte)'O', (byte)'N', (byte)'S', (byte)'E' };
            this.myMessage_10MB = RandomDataGenerator.GetBytes(10000000);
        }
    }
}


#endif