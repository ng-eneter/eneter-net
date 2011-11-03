
#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.MessagingSystems.Composites.ReliableMessagingComposit;

namespace Eneter.MessagingUnitTests.EndPoints.TypedSequencesReliableRequestResponse
{
    [TestFixture]
    public class Test_TypedSequencesReliable_Tcp_Bin : TypedSequencedReliableBaseTester
    {
        [SetUp]
        public void Setup()
        {
            IMessagingSystemFactory aMessagingSystem = new TcpMessagingSystemFactory();
            string aChannelId = "tcp://127.0.0.1:8050/";
            ISerializer aSerializer = new BinarySerializer();

            IReliableMessagingFactory aReliableMessaging = new ReliableMessagingFactory(aMessagingSystem, aSerializer, TimeSpan.FromMilliseconds(12000));

            Setup(aReliableMessaging, aChannelId, aSerializer);
        }
    }
}

#endif