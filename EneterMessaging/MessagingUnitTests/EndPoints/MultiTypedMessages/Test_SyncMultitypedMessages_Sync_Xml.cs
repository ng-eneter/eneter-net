using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.EndPoints.TypedMessages;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;

namespace Eneter.MessagingUnitTests.EndPoints.MultiTypedMessages
{
    [TestFixture]
    public class Test_SyncMultitypedMessages_Sync_Xml : SyncMultiTypedMessageBaseTester
    {
        [SetUp]
        public void Setup()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;

            IMessagingSystemFactory aMessaging = new SynchronousMessagingSystemFactory();
            InputChannel = aMessaging.CreateDuplexInputChannel("MyChannelId");
            OutputChannel = aMessaging.CreateDuplexOutputChannel("MyChannelId");

            MultiTypedMessagesFactory = new MultiTypedMessagesFactory();
        }
    }
}
