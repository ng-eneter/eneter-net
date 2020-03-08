using Eneter.Messaging.MessagingSystems.HttpMessagingSystem;
using NUnit.Framework;

namespace Eneter.MessagingUnitTests.MessagingSystems.HttpMessagingSystem
{
    [TestFixture]
    public class Test_HttpServer
    {
        [Test]
        public void StartListening()
        {
            HttpServer aHttpListener = new HttpServer("http://127.0.0.1:8094/");
            try
            {
                aHttpListener.StartListening(x =>
                {
                });

                Assert.IsTrue(aHttpListener.IsListening);
            }
            finally
            {
                aHttpListener.StopListening();
            }

            Assert.IsFalse(aHttpListener.IsListening);
        }
    }
}
