using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.ThreadMessagingSystem;
using Eneter.Messaging.DataProcessing.Serializing;

namespace Eneter.MessagingUnitTests.EndPoints.TypedMessages
{
    [TestFixture]
    public class Test_TypedMessages_ThreadMessaging_XmlStringSerializer : TypedMessagesBaseTester
    {
        [SetUp]
        public void Setup()
        {
            IMessagingSystemFactory aMessagingSystem = new ThreadMessagingSystemFactory();
            string aChannelId = "Channel1";
            ISerializer aSerializer = new XmlStringSerializer();

            Setup(aMessagingSystem, aChannelId, aSerializer);
        }
    }
}
