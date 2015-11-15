using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.NamedPipeMessagingSystem;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using System.IO;

namespace Eneter.MessagingUnitTests.EndPoints.TypedRequestResponse
{
    [TestFixture]
    public class Test_TypedRequestResponse_NamedPipe_Bin : TypedRequestResponseBaseTester
    {
        [SetUp]
        public void Setup()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            //EneterTrace.TraceLog = new StreamWriter("d:/tracefile.txt");

            IMessagingSystemFactory aMessagingSystem = new NamedPipeMessagingSystemFactory(2, 10000);

            // Generate random number for the port.
            string aChannelId = "pipe.net://127.0.0.1/Service1";

            ISerializer aSerializer = new BinarySerializer();

            Setup(aMessagingSystem, aChannelId, aSerializer);
        }

        [TearDown]
        public void TearDown()
        {
            if (EneterTrace.TraceLog != null)
            {
                EneterTrace.TraceLog.Dispose();
                EneterTrace.TraceLog = null;
            }
        }
    }
}
