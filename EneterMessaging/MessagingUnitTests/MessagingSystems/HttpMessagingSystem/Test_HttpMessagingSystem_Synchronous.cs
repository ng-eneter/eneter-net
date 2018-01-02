
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.HttpMessagingSystem;
using System.Net;
using Eneter.Messaging.Diagnostic;
using System.IO;

namespace Eneter.MessagingUnitTests.MessagingSystems.HttpMessagingSystem
{
    [TestFixture]
    public class Test_HttpMessagingSystem_Synchronous : HttpMessagingSystemBaseTester
    {
        [SetUp]
        public void Setup()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            //EneterTrace.TraceLog = new StreamWriter("d:/trace.txt");

            MessagingSystemFactory = new HttpMessagingSystemFactory();
            ChannelId = "http://127.0.0.1:8091/";
        }

    }
}