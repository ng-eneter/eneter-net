#if !SILVERLIGHT

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
    public class Test_GZipSerializer_Xml : SerializerTesterBase
    {
        [SetUp]
        public void Setup()
        {
            EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            EneterTrace.TraceLog = Console.Out;

            TestedSerializer = new GZipSerializer(new XmlStringSerializer());
            TestedSerializer2 = new GZipSerializer(new XmlStringSerializer());
        }

        [Test]
        public void SerializeDeserialize_10MB_GoodComprimation()
        {
            StringBuilder aStringBuilder = new StringBuilder();

            for (int i = 0; i < 10000000; ++i)
            {
                aStringBuilder.Append('a');
            }

            string aData = aStringBuilder.ToString();

            object aSerializedData = TestedSerializer.Serialize<string>(aData);
            EneterTrace.Info("Size of conpressed data: " + ((byte[])aSerializedData).Length.ToString());

            string aDeserializedData = TestedSerializer.Deserialize<string>(aSerializedData);

            Assert.AreEqual(aData, aDeserializedData);
        }
    }
}


#endif