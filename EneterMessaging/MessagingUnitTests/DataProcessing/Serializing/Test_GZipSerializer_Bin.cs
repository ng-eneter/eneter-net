
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;

namespace Eneter.MessagingUnitTests.DataProcessing.Serializing
{
    [TestFixture]
    public class Test_GZipSerializer_Bin : SerializerTesterBase
    {
        [SetUp]
        public void Setup()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            //EneterTrace.TraceLog = Console.Out;

            TestedSerializer = new GZipSerializer(new BinarySerializer());
            TestedSerializer2 = new GZipSerializer(new BinarySerializer());
        }
    }
}