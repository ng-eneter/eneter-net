using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;
using Eneter.Messaging.EndPoints.TypedMessages;
using Eneter.Messaging.Diagnostic;

namespace Eneter.MessagingUnitTests.EndPoints.SyncTypedMessages
{
    [TestFixture]
    public class Test_SyncTypedMessages_Sync_Xml : SyncTypedMessagesBaseTester
    {
        [SetUp]
        public void Setup()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;

            IMessagingSystemFactory aMessaging = new SynchronousMessagingSystemFactory();
            InputChannel = aMessaging.CreateDuplexInputChannel("MyChannelId");
            OutputChannel = aMessaging.CreateDuplexOutputChannel("MyChannelId");

            DuplexTypedMessagesFactory = new DuplexTypedMessagesFactory();
        }
    }
}
