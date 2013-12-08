
#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.HttpMessagingSystem;
using System.Net;
using System.IO;

namespace Eneter.MessagingUnitTests.MessagingSystems.HttpMessagingSystem
{
    [TestFixture]
    public class Test_HttpPolicyServer
    {
        [Test]
        public void TestPolicy()
        {
            // Start policy server.
            HttpPolicyServer aPolicyServer = new HttpPolicyServer("http://127.0.0.1:8055/");
            try
            {
                aPolicyServer.StartPolicyServer();

                // Request policy xml.
                HttpWebRequest aWebRequest = (HttpWebRequest)WebRequest.Create("http://127.0.0.1:8055/clientaccesspolicy.xml");
                aWebRequest.Method = "GET";

                // Send http request.
                WebResponse aResponse = aWebRequest.GetResponse();


                string aPolicyXml = null;

                using (MemoryStream aBuf = new MemoryStream())
                {
                    Stream anInputStream = aResponse.GetResponseStream();
                    int aSize = 0;
                    byte[] aBuffer = new byte[32768];
                    while ((aSize = anInputStream.Read(aBuffer, 0, aBuffer.Length)) != 0)
                    {
                        aBuf.Write(aBuffer, 0, aSize);
                    }

                    aPolicyXml = Encoding.UTF8.GetString(aBuf.ToArray());
                }


                Assert.AreEqual(aPolicyServer.PolicyXml, aPolicyXml);
            }
            finally
            {
                aPolicyServer.StopPolicyServer();
            }
        }
    }
}


#endif