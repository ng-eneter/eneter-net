using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;

namespace Eneter.MessagingUnitTests.EndPoints.Rpc
{
    [TestFixture]
    public class Test_Rpc_Sync_Xml : RpcBaseTester
    {
        [SetUp]
        public void Setup()
        {
            mySerializer = new XmlStringSerializer();
            myChannelId = "channel_1";
            myMessaging = new SynchronousMessagingSystemFactory();
        }

        public override void RpcTimeout()
        {
            // N.A.
        }
    }
}
