using Eneter.Messaging.MessagingSystems.HttpMessagingSystem;
using NUnit.Framework;
using System;
using System.Net;

namespace Eneter.MessagingUnitTests.MessagingSystems.HttpMessagingSystem
{
    [TestFixture]
    public class Test_HttpWebClient
    {
        [Test]
        public void Get()
        {
            HttpWebServer aHttpListener = new HttpWebServer("http://127.0.0.1:8094/");
            try
            {
                aHttpListener.StartListening(x =>
                {
                    if (x.Request.HttpMethod == "GET")
                    {
                        x.SendResponseMessage("blabla");
                    }
                    else
                    {
                        x.Response.StatusCode = 404;
                    }
                });

                HttpWebResponse aWebResponse = HttpWebClient.Get(new Uri("http://127.0.0.1:8094/hello/"));
                Assert.AreEqual(HttpStatusCode.OK, aWebResponse.StatusCode);

                string aResponseMessage = aWebResponse.GetResponseMessageStr();

                Assert.AreEqual("blabla", aResponseMessage);
            }
            finally
            {
                aHttpListener.StopListening();
            }
        }

        [Test]
        public void Get_NotFound()
        {
            HttpWebServer aHttpListener = new HttpWebServer("http://127.0.0.1:8094/");
            try
            {
                aHttpListener.StartListening(x =>
                {
                    if (x.Request.HttpMethod == "GET")
                    {
                        x.Response.StatusCode = 404;
                    }
                });

                Assert.Throws<WebException>(() => HttpWebClient.Get(new Uri("http://127.0.0.1:8094/hello/")));
            }
            finally
            {
                aHttpListener.StopListening();
            }
        }

        [Test]
        public void Delete()
        {
            HttpWebServer aHttpListener = new HttpWebServer("http://127.0.0.1:8094/");
            try
            {
                aHttpListener.StartListening(x =>
                {
                    if (x.Request.HttpMethod == "DELETE")
                    {
                        // OK
                        //x.Response.StatusCode = 200;
                    }
                    else
                    {
                        x.Response.StatusCode = 404;
                    }
                });

                HttpWebResponse aWebResponse = HttpWebClient.Delete(new Uri("http://127.0.0.1:8094/hello/"));
                Assert.AreEqual(HttpStatusCode.OK, aWebResponse.StatusCode);
            }
            finally
            {
                aHttpListener.StopListening();
            }
        }

        [Test]
        public void Post()
        {
            HttpWebServer aHttpListener = new HttpWebServer("http://127.0.0.1:8094/");
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
                    else
                    {
                        x.Response.StatusCode = 404;
                    }
                });

                HttpWebResponse aWebResponse = HttpWebClient.Post(new Uri("http://127.0.0.1:8094/hello/"), "abcd");
                Assert.AreEqual(HttpStatusCode.OK, aWebResponse.StatusCode);

                string aResponseMessage = aWebResponse.GetResponseMessageStr();

                Assert.AreEqual("abcd", aReceivedRequest);
                Assert.AreEqual("blabla", aResponseMessage);
            }
            finally
            {
                aHttpListener.StopListening();
            }
        }

        [Test]
        public void Put()
        {
            HttpWebServer aHttpListener = new HttpWebServer("http://127.0.0.1:8094/");
            try
            {
                object aLock = new object();
                string aReceivedRequest = null;

                aHttpListener.StartListening(x =>
                {
                    if (x.Request.HttpMethod == "PUT")
                    {
                        lock (aLock)
                        {
                            aReceivedRequest = x.GetRequestMessageStr();
                        }

                        x.SendResponseMessage("blabla");
                    }
                    else
                    {
                        x.Response.StatusCode = 404;
                    }
                });

                HttpWebResponse aWebResponse = HttpWebClient.Put(new Uri("http://127.0.0.1:8094/hello/"), "abcd");
                Assert.AreEqual(HttpStatusCode.OK, aWebResponse.StatusCode);

                string aResponseMessage = aWebResponse.GetResponseMessageStr();

                Assert.AreEqual("abcd", aReceivedRequest);
                Assert.AreEqual("blabla", aResponseMessage);
            }
            finally
            {
                aHttpListener.StopListening();
            }
        }

        [Test]
        public void Patch()
        {
            HttpWebServer aHttpListener = new HttpWebServer("http://127.0.0.1:8094/");
            try
            {
                object aLock = new object();
                string aReceivedRequest = null;

                aHttpListener.StartListening(x =>
                {
                    if (x.Request.HttpMethod == "PATCH")
                    {
                        lock (aLock)
                        {
                            aReceivedRequest = x.GetRequestMessageStr();
                        }

                        x.SendResponseMessage("blabla");
                    }
                    else
                    {
                        x.Response.StatusCode = 404;
                    }
                });

                HttpWebResponse aWebResponse = HttpWebClient.Patch(new Uri("http://127.0.0.1:8094/hello/"), "abcd");
                Assert.AreEqual(HttpStatusCode.OK, aWebResponse.StatusCode);

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
