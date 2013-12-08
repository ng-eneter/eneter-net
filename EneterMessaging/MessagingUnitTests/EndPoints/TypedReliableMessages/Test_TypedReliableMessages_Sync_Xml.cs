using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;
using Eneter.Messaging.DataProcessing.Serializing;

namespace Eneter.MessagingUnitTests.EndPoints.TypedReliableMessages
{
    [TestFixture]
    public class Test_TypedReliableMessages_Sync_Xml : TypedReliableMessagesBaseTester
    {
        [SetUp]
        public void Setup()
        {
            IMessagingSystemFactory aMessagingSystem = new SynchronousMessagingSystemFactory();

            string aChannelId = "Channel1";
            ISerializer aSerializer = new XmlStringSerializer();

            Setup(aMessagingSystem, aChannelId, aSerializer);
        }
    }
}
