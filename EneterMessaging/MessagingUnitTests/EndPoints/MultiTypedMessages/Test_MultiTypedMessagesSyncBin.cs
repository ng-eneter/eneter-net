using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;
using Eneter.Messaging.Diagnostic;
using System.IO;

namespace Eneter.MessagingUnitTests.EndPoints.MultiTypedMessages
{
    [TestFixture]
    public class Test_MultiTypedMessagesSyncBin : MultiTypedMessagesBaseTester
    {
        [SetUp]
        public void Setup()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            //EneterTrace.TraceLog = new StreamWriter("d:/tracefile.txt");

            IMessagingSystemFactory aMessagingSystem = new SynchronousMessagingSystemFactory();
            string aChannelId = "Channel1";
            ISerializer aSerializer = new BinarySerializer();

            Setup(aMessagingSystem, aChannelId, aSerializer);
        }
    }
}
