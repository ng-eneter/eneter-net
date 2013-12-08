
#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.NamedPipeMessagingSystem;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using System.Threading;
using Eneter.Messaging.Diagnostic;
using System.IO;

namespace Eneter.MessagingUnitTests.MessagingSystems.NamedPipeMessagingSystem
{
    [TestFixture]
    public class Test_NamedPipeMessagingLocalMachine : NamedPipeMessagingSystemBaseTester
    {
        [SetUp]
        public void Setup()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            //EneterTrace.TraceLog = new StreamWriter("d:/tracefile.txt");

            MessagingSystemFactory = new NamedPipeMessagingSystemFactory(10, 10000);
            ChannelId = "net.pipe://127.0.0.1/bbb";
        }

        
    }
}

#endif