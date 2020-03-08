using Eneter.Messaging.MessagingSystems.HttpMessagingSystem;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Eneter.MessagingUnitTests.MessagingSystems.HttpMessagingSystem
{
    [TestFixture]
    public class Test_HttpClient
    {
        [Test]
        public void Get()
        {
            HttpServer aHttpListener = new HttpServer("http://127.0.0.1:8094/");
            try
            {
                aHttpListener.StartListening(x =>
                {
                    if (x.Request.HttpMethod == "GET")
                    {
                        x.SendResponseMessage("blabla");
                    }
                });

                HttpWebResponse aWebResponse = HttpClient.Get(new Uri("http://127.0.0.1:8094/hello/"));

                string aResponseMessage = aWebResponse.GetResponseMessageStr();

                Assert.AreEqual("blabla", aResponseMessage);
            }
            finally
            {
                aHttpListener.StopListening();
            }
        }

        [Test]
        public void Post()
        {
            HttpServer aHttpListener = new HttpServer("http://127.0.0.1:8094/");
            try
            {
                object aLock = new object();
                string aReceivedRequest = null;

                aHttpListener.StartListening(x =>
                {
                    if (x.Request.HttpMethod == "POST")
                    {
                        lock (aLock)
                        {
                            aReceivedRequest = x.GetRequestMessageStr();
                        }

                        x.SendResponseMessage("blabla");
                    }
                });

                HttpWebResponse aWebResponse = HttpClient.Post(new Uri("http://127.0.0.1:8094/hello/"), "abcd");

                string aResponseMessage = aWebResponse.GetResponseMessageStr();

                Assert.AreEqual("abcd", aReceivedRequest);
                Assert.AreEqual("blabla", aResponseMessage);
            }
            finally
            {
                aHttpListener.StopListening();
            }
        }
    }
}
