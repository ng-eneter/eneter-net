using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.MessagingSystems.Composites.ReliableMessagingComposit;

namespace Eneter.MessagingUnitTests.EndPoints.TypedSequencesReliableRequestResponse
{
    [TestFixture]
    public class Test_TypedSequencesReliable_Sync_Xml : TypedSequencedReliableBaseTester
    {
        [SetUp]
        public void Setup()
        {
            IMessagingSystemFactory aMessagingSystem = new SynchronousMessagingSystemFactory();
            string aChannelId = "Channel1";
            ISerializer aSerializer = new XmlStringSerializer();

            IReliableMessagingFactory aReliableMessaging = new ReliableMessagingFactory(aMessagingSystem);

            Setup(aReliableMessaging, aChannelId, aSerializer);
        }
    }
}
