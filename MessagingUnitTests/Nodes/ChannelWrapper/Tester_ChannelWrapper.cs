using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.Nodes.ChannelWrapper;
using Eneter.Messaging.EndPoints.StringMessages;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;
using Eneter.Messaging.Infrastructure.ConnectionProvider;


namespace Eneter.MessagingUnitTests.Nodes.ChannelWrapper
{
    [TestFixture]
    public class Tester_ChannelWrapper
    {
        [SetUp]
        public void Setup()
        {
            IMessagingSystemFactory aGlobalChannelFactory = new SynchronousMessagingSystemFactory();
            myGlobalOutputChannel = aGlobalChannelFactory.CreateOutputChannel("MainChannel");
            myGlobalInputChannel = aGlobalChannelFactory.CreateInputChannel("MainChannel");

            IConnectionProviderFactory aConnectionProviderFactory = new ConnectionProviderFactory();
            myConnectionProviderForWrapper = aConnectionProviderFactory.CreateConnectionProvider(new SynchronousMessagingSystemFactory());
            myConnectionProviderForUnwrapper = aConnectionProviderFactory.CreateConnectionProvider(new SynchronousMessagingSystemFactory());

            IChannelWrapperFactory aChannelWrapperFactory = new ChannelWrapperFactory();
            myChannelWrapper = aChannelWrapperFactory.CreateChannelWrapper();
            myChannelUnwrapper = aChannelWrapperFactory.CreateChannelUnwrapper(myConnectionProviderForUnwrapper.MessagingSystem);
        }

        [Test]
        public void WrapUnwrapChannels()
        {
            // Wrapped/unwrapped channels
            string aChannel1Id = "Channel1Id";
            string aChannel2Id = "Channel2Id";
           
            IStringMessagesFactory aStringMessagesFactory = new StringMessagesFactory();

            // Create String senders and receivers
            IStringMessageSender aStringMessageSender1 = aStringMessagesFactory.CreateStringMessageSender();
            IStringMessageSender aStringMessageSender2 = aStringMessagesFactory.CreateStringMessageSender();

            IStringMessageReceiver aStringMessageReceiver1 = aStringMessagesFactory.CreateStringMessageReceiver();
            IStringMessageReceiver aStringMessageReceiver2 = aStringMessagesFactory.CreateStringMessageReceiver();

            // Connect string senders with the channel wrapper.
            myConnectionProviderForWrapper.Connect(myChannelWrapper, aStringMessageSender1, aChannel1Id);
            myConnectionProviderForWrapper.Connect(myChannelWrapper, aStringMessageSender2, aChannel2Id);

            // Attach input channels to string receivers.
            myConnectionProviderForUnwrapper.Attach(aStringMessageReceiver1, aChannel1Id);
            myConnectionProviderForUnwrapper.Attach(aStringMessageReceiver2, aChannel2Id);

            // Attach input output to the wrapper and unwrapper
            myChannelWrapper.AttachOutputChannel(myGlobalOutputChannel);
            myChannelUnwrapper.AttachInputChannel(myGlobalInputChannel);


            // Observing string message receivers
            string aReceivedMessage1 = "";
            aStringMessageReceiver1.MessageReceived += (x, y) =>
            {
                aReceivedMessage1 = y.Message;
            };

            string aReceivedMessage2 = "";
            aStringMessageReceiver2.MessageReceived += (x, y) =>
            {
                aReceivedMessage2 = y.Message;
            };

            // Send the first message
            string aMessage1 = "Message1";
            aStringMessageSender1.SendMessage(aMessage1);

            // Check
            Assert.AreEqual(aMessage1, aReceivedMessage1);
            Assert.AreEqual("", aReceivedMessage2);

            // Send the second message
            aReceivedMessage1 = "";
            aReceivedMessage2 = "";

            string aMessage2 = "Message2";
            aStringMessageSender2.SendMessage(aMessage2);

            // Check
            Assert.AreEqual("", aReceivedMessage1);
            Assert.AreEqual(aMessage2, aReceivedMessage2);
        }

        private IConnectionProvider myConnectionProviderForWrapper;
        private IConnectionProvider myConnectionProviderForUnwrapper;

        private IChannelWrapper myChannelWrapper;
        private IChannelUnwrapper myChannelUnwrapper;

        private IOutputChannel myGlobalOutputChannel;
        private IInputChannel myGlobalInputChannel;
    }
}
