using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;
using System.Threading;
using Eneter.Messaging.Diagnostic;
using System.Text.RegularExpressions;
using System.IO;


namespace Eneter.MessagingUnitTests.MessagingSystems.SynchronousMessagingSystem
{
    [TestFixture]
    public class Test_SynchronousMessagingSystem : BaseTester
    {
        [SetUp]
        public void Setup()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            //EneterTrace.TraceLog = new StreamWriter("d:/tracefile.txt");

            MessagingSystemFactory = new SynchronousMessagingSystemFactory();
        }

        //[TearDown]
        //public void Clean()
        //{
        //    EneterTrace.StopProfiler();
        //}
    
    }
}
