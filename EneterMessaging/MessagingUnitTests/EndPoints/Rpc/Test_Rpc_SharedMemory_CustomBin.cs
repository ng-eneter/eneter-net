#if !COMPACT_FRAMEWORK

using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.EndPoints.Rpc;
using Eneter.Messaging.MessagingSystems.SharedMemoryMessagingSystem;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Eneter.MessagingUnitTests.EndPoints.Rpc
{
    [TestFixture]
    public class Test_Rpc_SharedMemory_CustomBin : RpcBaseTester
    {
        [SetUp]
        public void Setup()
        {
            mySerializer = new RpcCustomSerializer(new BinarySerializer());
            myChannelId = "MySharedMemoryAddress";
            myMessaging = new SharedMemoryMessagingSystemFactory();
        }
    }
}

#endif