
#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
using Eneter.Messaging.DataProcessing.Serializing;

namespace Eneter.MessagingUnitTests.EndPoints.TypedSequencedRequestResponse
{
    [TestFixture]
    public class Test_TypedSequencedRequestResponse_TcpMessaging_XmlSerializer : TypedSequencedRequestResponseBaseTester
    {
        [SetUp]
        public void Setup()
        {
            IMessagingSystemFactory aMessagingSystem = new TcpMessagingSystemFactory();
            string aChannelId = "tcp://127.0.0.1:8090/";
            ISerializer aSerializer = new XmlStringSerializer();

            Setup(aMessagingSystem, aChannelId, aSerializer);
        }
    }
}

#endif