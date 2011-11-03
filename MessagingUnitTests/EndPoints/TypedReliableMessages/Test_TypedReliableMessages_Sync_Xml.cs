using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.MessagingSystems.Composites.ReliableMessagingComposit;

namespace Eneter.MessagingUnitTests.EndPoints.TypedReliableMessages
{
    [TestFixture]
    public class Test_TypedReliableMessages_Sync_Xml : TypedReliableMessagesBaseTester
    {
        [SetUp]
        public void Setup()
        {
            IMessagingSystemFactory anUnderlyingMessagingSystem = new SynchronousMessagingSystemFactory();
            IReliableMessagingFactory aReliableMessaging = new ReliableMessagingFactory(anUnderlyingMessagingSystem);

            string aChannelId = "Channel1";
            ISerializer aSerializer = new XmlStringSerializer();

            Setup(aReliableMessaging, aChannelId, aSerializer);
        }
    }
}
