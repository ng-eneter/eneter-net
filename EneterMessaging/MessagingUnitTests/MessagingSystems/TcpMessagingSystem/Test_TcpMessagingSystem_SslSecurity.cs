
#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security;
using System.Security.Cryptography.X509Certificates;
using System.Resources;
using Eneter.MessagingUnitTests.Properties;
using System.IO;
using System.Net.Security;

namespace Eneter.MessagingUnitTests.MessagingSystems.TcpMessagingSystem
{
    [TestFixture]
    public class Test_TcpMessagingSystem_SslSecurity : Test_TcpMessagingSystem_Synchronous
    {
        [SetUp]
        public new void Setup()
        {
            ResourceManager aResourceManager = new ResourceManager("Eneter.MessagingUnitTests.Properties.Resources", typeof(Resources).Assembly);

            // Get the server certificate from the resources.
            byte[] aServerCertBytes = (byte[])aResourceManager.GetObject("MyLocalCertificate");
            X509Certificate aServerCertificate = new X509Certificate(aServerCertBytes);

            // Get the client certificate from the resources.
            byte[] aClientCertBytes = (byte[])aResourceManager.GetObject("MyClientCertificate");
            X509Certificate aClientCertificate = new X509Certificate(aClientCertBytes);
            X509CertificateCollection aClientCertList = new X509CertificateCollection();
            aClientCertList.Add(aClientCertificate);

            MessagingSystemFactory = new TcpMessagingSystemFactory()
            {
                ClientSecurityStreamFactory = new ClientSslFactory("127.0.0.1", aClientCertList),
                ServerSecurityStreamFactory = new ServerSslFactory(aServerCertificate, false)
            };

            ChannelId = "tcp://127.0.0.1:8091/";
        }
    }
}

#endif