using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.DataProcessing.Wrapping;
using Eneter.Messaging.DataProcessing.Serializing;

namespace Eneter.MessagingUnitTests.DataProcessing.Wrapping
{

    [TestFixture]
    public class Test_DataWrapper
    {
        [Test]
        public void WrapUnwrap()
        {
            XmlStringSerializer aSerializer = new XmlStringSerializer();

            DateTime aTime = DateTime.Now;
            string anOriginalySerializedData = "Some serialized data";

            // Serialize
            object aSerializedData = DataWrapper.Wrap(aTime, anOriginalySerializedData, aSerializer);

            // Deserialize
            WrappedData aWrappedData = DataWrapper.Unwrap(aSerializedData, aSerializer);

            Assert.AreEqual(aTime, (DateTime)aWrappedData.AddedData);
            Assert.AreEqual(anOriginalySerializedData, aWrappedData.OriginalData as string);
        }
    }
}
