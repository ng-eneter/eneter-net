using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.DataProcessing.Serializing;
using System.Security.Cryptography.X509Certificates;
using System.Resources;
using Eneter.MessagingUnitTests.Properties;

namespace Eneter.MessagingUnitTests.DataProcessing.Serializing
{
    [TestFixture]
    public class Test_RsaDigitalSignatureSerializer_Xml : SerializerTesterBase
    {
        [SetUp]
        public void Setup()
        {
            // Get the server certificate from the resources.
            ResourceManager aResourceManager = new ResourceManager("Eneter.MessagingUnitTests.Properties.Resources", typeof(Resources).Assembly);
            byte[] aServerCertBytes = (byte[])aResourceManager.GetObject("EneterSigner");
            X509Certificate2 aServerCertificate = new X509Certificate2(aServerCertBytes, "alicka");

            //X509Certificate2 aServerCertificate = new X509Certificate2("d:/EneterSigner.pfx", "alicka");

            TestedSerializer = new RsaDigitalSignatureSerializer(aServerCertificate, x => true, new XmlStringSerializer());
            TestedSerializer2 = new RsaDigitalSignatureSerializer(aServerCertificate, x => true, new XmlStringSerializer());
        }
    }
}
