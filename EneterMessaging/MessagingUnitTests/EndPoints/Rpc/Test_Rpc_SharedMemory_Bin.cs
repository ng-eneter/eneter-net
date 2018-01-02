
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.MessagingSystems.SharedMemoryMessagingSystem;

namespace Eneter.MessagingUnitTests.EndPoints.Rpc
{
    [TestFixture]
    public class Test_Rpc_SharedMemory_Bin : RpcBaseTester
    {
        [SetUp]
        public void Setup()
        {
            mySerializer = new BinarySerializer();
            myChannelId = "MySharedMemoryAddress";
            myMessaging = new SharedMemoryMessagingSystemFactory();
        }
    }
}
