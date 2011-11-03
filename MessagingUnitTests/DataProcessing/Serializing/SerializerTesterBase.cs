using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eneter.Messaging.DataProcessing.Serializing;
using NUnit.Framework;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace Eneter.MessagingUnitTests.DataProcessing.Serializing
{
    public abstract class SerializerTesterBase
    {
#if !SILVERLIGHT
        [Serializable]
#endif
        [DataContract]
        public class T1
        {
            [DataMember]
            public int x;

            [DataMember]
            public int y;
        }

#if !SILVERLIGHT
        [Serializable]
#endif
        [DataContract]
        public class T2
        {
            [DataMember]
            public string a;

            [DataMember]
            public object o;
        }

        [Test]
        public void SerializeDeserialize()
        {
            string aData = "hello world";
            object aSerializedData = TestedSerializer.Serialize<string>(aData);
            string aDeserializedData = TestedSerializer.Deserialize<string>(aSerializedData);

            Assert.AreEqual(aData, aDeserializedData);
        }

        [Test]
        public void SerializeDeserialize_Recursive()
        {
            T1 t1 = new T1();
            t1.x = 1;
            t1.y = 2;

            object aSerializedT1 = TestedSerializer.Serialize<T1>(t1);

            T2 t2 = new T2();
            t2.a = "hello";
            t2.o = aSerializedT1;

            object aSerializedT2 = TestedSerializer.Serialize<T2>(t2);


            T2 aDeserializedT2 = TestedSerializer.Deserialize<T2>(aSerializedT2);
            Assert.AreEqual(t2.a, aDeserializedT2.a);

            T1 aDeserializedT1 = TestedSerializer.Deserialize<T1>(aSerializedT1);
            Assert.AreEqual(t1.x, aDeserializedT1.x);
            Assert.AreEqual(t1.y, aDeserializedT1.y);
        }

        [Test]
        public void SerializeDeserialize_10MB()
        {
            // 10MB
            string aData = RandomDataGenerator.GetString(10000000);
            object aSerializedData = TestedSerializer.Serialize<string>(aData);
            string aDeserializedData = TestedSerializer.Deserialize<string>(aSerializedData);

            Assert.AreEqual(aData, aDeserializedData);
        }

        [Test]
        public void SerializeDeserialize_2SerializerInstances_10MB()
        {
            // 10MB
            string aData = RandomDataGenerator.GetString(10000000);
            object aSerializedData = TestedSerializer.Serialize<string>(aData);
            string aDeserializedData = TestedSerializer2.Deserialize<string>(aSerializedData);

            Assert.AreEqual(aData, aDeserializedData);
        }

        //[Test]
        public void SerializeDeserialize_10MB_500x()
        {
            // 10MB
            string aData = RandomDataGenerator.GetString(10000000);

            for (int i = 0; i < 500; ++i)
            {
                object aSerializedData = TestedSerializer.Serialize<string>(aData);
                string aDeserializedData = TestedSerializer.Deserialize<string>(aSerializedData);
                Assert.AreEqual(aData, aDeserializedData, "Iteration " + i.ToString() + " failed.");
            }
        }

        protected ISerializer TestedSerializer { get; set; }

        /// <summary>
        /// Used if the deserialization should be done by a different instance.
        /// </summary>
        protected ISerializer TestedSerializer2 { get; set; }
    }
}
