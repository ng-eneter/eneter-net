
#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.SharedMemoryMessagingSystem;
using Eneter.Messaging.Diagnostic;
using System.IO;
using System.Threading;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.MessagingUnitTests.MessagingSystems.SharedMemoryMessagingSystem
{
    [TestFixture]
    public class Test_SharedMemoryMessagingSystem : BaseTester
    {
        [SetUp]
        public void Setup()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            //EneterTrace.TraceLog = new StreamWriter("d:/tracefile.txt");

            MessagingSystemFactory = new SharedMemoryMessagingSystemFactory()
            {
                ConnectTimeout = TimeSpan.FromMilliseconds(3000)
            };

            ChannelId = "Channel1";
        }

    }
}

#endif