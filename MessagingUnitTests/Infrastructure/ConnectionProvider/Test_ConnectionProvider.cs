using Eneter.Messaging.Infrastructure.ConnectionProvider;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;
using NUnit.Framework;
using Eneter.Messaging.Infrastructure.Attachable;

namespace Eneter.MessagingUnitTests.Infrastructure.ConnectionProvider
{
    [TestFixture]
    public class Test_ConnectionProvider
    {
        [SetUp]
        public void Setup()
        {
            myMessagingSystemFactory = new SynchronousMessagingSystemFactory();
            myConnectionProviderFactory = new ConnectionProviderFactory();
        }

        [Test]
        public void Connect()
        {
            IConnectionProvider aConnectionProvider = myConnectionProviderFactory.CreateConnectionProvider(myMessagingSystemFactory);

            Mock_Attachable a1 = new Mock_Attachable();

            string aReceivedInputChannel = "";
            a1.InputChannelAttached += (x, y) =>
                {
                    aReceivedInputChannel = y.ChannelId;
                };

            string aReceivedOutputChannel = "";
            a1.OutputChannelAttached += (x, y) =>
                {
                    aReceivedOutputChannel = y.ChannelId;
                };


            aConnectionProvider.Connect((IAttachableInputChannel)a1, (IAttachableOutputChannel)a1, "Channel1");
            Assert.AreEqual("Channel1", aReceivedInputChannel);
            Assert.AreEqual("Channel1", aReceivedOutputChannel);


            aConnectionProvider.Connect((IAttachableMultipleInputChannels)a1, (IAttachableOutputChannel)a1, "Channel2");
            Assert.AreEqual("Channel2", aReceivedInputChannel);
            Assert.AreEqual("Channel2", aReceivedOutputChannel);

            aConnectionProvider.Connect((IAttachableMultipleInputChannels)a1, (IAttachableMultipleOutputChannels)a1, "Channel3");
            Assert.AreEqual("Channel3", aReceivedInputChannel);
            Assert.AreEqual("Channel3", aReceivedOutputChannel);
        }

        private IMessagingSystemFactory myMessagingSystemFactory;
        private IConnectionProviderFactory myConnectionProviderFactory;
    }
}
