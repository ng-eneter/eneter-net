using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.Nodes.ChannelWrapper;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;
using Eneter.Messaging.Infrastructure.ConnectionProvider;
using Eneter.Messaging.EndPoints.StringMessages;

namespace Eneter.MessagingUnitTests.Nodes.ChannelWrapper
{
    [TestFixture]
    public class Tester_DuplexChannelWrapper
    {
        [SetUp]
        public void Setup()
        {
            IMessagingSystemFactory aGlobalChannelFactory = new SynchronousMessagingSystemFactory();
            myDuplexGlobalOutputChannel = aGlobalChannelFactory.CreateDuplexOutputChannel("MainChannel");
            myDuplexGlobalInputChannel = aGlobalChannelFactory.CreateDuplexInputChannel("MainChannel");

            IConnectionProviderFactory aConnectionProviderFactory = new ConnectionProviderFactory();
            myConnectionProviderForWrapper = aConnectionProviderFactory.CreateConnectionProvider(new SynchronousMessagingSystemFactory());
            myConnectionProviderForUnwrapper = aConnectionProviderFactory.CreateConnectionProvider(new SynchronousMessagingSystemFactory());

            IChannelWrapperFactory aFactory = new ChannelWrapperFactory();
            myDuplexChannelWrapper = aFactory.CreateDuplexChannelWrapper();
            myDuplexChannelUnwrapper = aFactory.CreateDuplexChannelUnwrapper(myConnectionProviderForUnwrapper.MessagingSystem);
        }

        [Test]
        public void WrapUnwrapMessage()
        {
            // Wrapped/unwrapped channels
            string aChannel1Id = "Channel1Id";
            string aChannel2Id = "Channel2Id";

            IDuplexStringMessagesFactory aStringMessagesFactory = new DuplexStringMessagesFactory();
            
            IDuplexStringMessageReceiver aStringMessageReceiver1 = aStringMessagesFactory.CreateDuplexStringMessageReceiver();
            IDuplexStringMessageReceiver aStringMessageReceiver2 = aStringMessagesFactory.CreateDuplexStringMessageReceiver();

            IDuplexStringMessageSender aStringMessageSender1 = aStringMessagesFactory.CreateDuplexStringMessageSender();
            IDuplexStringMessageSender aStringMessageSender2 = aStringMessagesFactory.CreateDuplexStringMessageSender();

            // Attach input channels to string receivers.
            myConnectionProviderForUnwrapper.Attach(aStringMessageReceiver1, aChannel1Id);
            myConnectionProviderForUnwrapper.Attach(aStringMessageReceiver2, aChannel2Id);

            // Connect string senders with the channel wrapper.
            myConnectionProviderForWrapper.Connect(myDuplexChannelWrapper, aStringMessageSender1, aChannel1Id);
            myConnectionProviderForWrapper.Connect(myDuplexChannelWrapper, aStringMessageSender2, aChannel2Id);

            // Connect wrapper and unwrapper to global channels.
            myDuplexChannelUnwrapper.AttachDuplexInputChannel(myDuplexGlobalInputChannel);
            myDuplexChannelWrapper.AttachDuplexOutputChannel(myDuplexGlobalOutputChannel);


            StringRequestReceivedEventArgs aReceivedMessage1 = null;
            aStringMessageReceiver1.RequestReceived += (x, y) =>
                {
                    aReceivedMessage1 = y;
                    aStringMessageReceiver1.SendResponseMessage(y.ResponseReceiverId, "Response1");
                };

            StringRequestReceivedEventArgs aReceivedMessage2 = null;
            aStringMessageReceiver2.RequestReceived += (x, y) =>
                {
                    aReceivedMessage2 = y;
                    aStringMessageReceiver2.SendResponseMessage(y.ResponseReceiverId, "Response2");
                };

            StringResponseReceivedEventArgs aReceivedResponse1 = null;
            aStringMessageSender1.ResponseReceived += (x, y) =>
                {
                    aReceivedResponse1 = y;
                };

            StringResponseReceivedEventArgs aReceivedResponse2 = null;
            aStringMessageSender2.ResponseReceived += (x, y) =>
                {
                    aReceivedResponse2 = y;
                };

            aStringMessageSender1.SendMessage("Message1");

            Assert.AreEqual("Message1", aReceivedMessage1.RequestMessage, "Message receiver 1 received incorrect message.");
            Assert.AreEqual("Response1", aReceivedResponse1.ResponseMessage, "Response receiver 1 received incorrect message.");
            Assert.IsNull(aReceivedMessage2, "Message receiver 2 should not receive a message.");
            Assert.IsNull(aReceivedResponse2, "Response receiver 2 should not receive a message.");

            string anAssociatedResponseReceiverId = myDuplexChannelUnwrapper.GetAssociatedResponseReceiverId(aReceivedMessage1.ResponseReceiverId);
            Assert.AreEqual(myDuplexChannelWrapper.AttachedDuplexOutputChannel.ResponseReceiverId, anAssociatedResponseReceiverId);


            aReceivedMessage1 = null;
            aReceivedResponse1 = null;

            aStringMessageSender2.SendMessage("Message2");

            Assert.AreEqual("Message2", aReceivedMessage2.RequestMessage, "Message receiver 2 received incorrect message.");
            Assert.AreEqual("Response2", aReceivedResponse2.ResponseMessage, "Response receiver 2 received incorrect message.");
            Assert.IsNull(aReceivedMessage1, "Message receiver 1 should not receive a message.");
            Assert.IsNull(aReceivedResponse1, "Response receiver 1 should not receive a message.");
        }


        //[Test]
        public void AssociatedResponseReceiverId()
        {
            // Wrapped/unwrapped channels
            string aChannel1Id = "Channel1Id";
            string aChannel2Id = "Channel2Id";

            IDuplexStringMessagesFactory aStringMessagesFactory = new DuplexStringMessagesFactory();

            IDuplexStringMessageReceiver aStringMessageReceiver1 = aStringMessagesFactory.CreateDuplexStringMessageReceiver();
            IDuplexStringMessageReceiver aStringMessageReceiver2 = aStringMessagesFactory.CreateDuplexStringMessageReceiver();

            IDuplexStringMessageSender aStringMessageSender1 = aStringMessagesFactory.CreateDuplexStringMessageSender();
            IDuplexStringMessageSender aStringMessageSender2 = aStringMessagesFactory.CreateDuplexStringMessageSender();

            // Attach input channels to string receivers.
            myConnectionProviderForUnwrapper.Attach(aStringMessageReceiver1, aChannel1Id);
            myConnectionProviderForUnwrapper.Attach(aStringMessageReceiver2, aChannel2Id);

            // Connect string senders with the channel wrapper.
            myConnectionProviderForWrapper.Connect(myDuplexChannelWrapper, aStringMessageSender1, aChannel1Id);
            myConnectionProviderForWrapper.Connect(myDuplexChannelWrapper, aStringMessageSender2, aChannel2Id);

            try
            {
                // Connect wrapper and unwrapper to global channels.
                myDuplexChannelUnwrapper.AttachDuplexInputChannel(myDuplexGlobalInputChannel);
                myDuplexChannelWrapper.AttachDuplexOutputChannel(myDuplexGlobalOutputChannel);


                StringRequestReceivedEventArgs aReceivedMessage1 = null;
                aStringMessageReceiver1.RequestReceived += (x, y) =>
                    {
                        aReceivedMessage1 = y;
                    };
                bool aResponseReceiverChannel1Disconnected = false;
                aStringMessageReceiver1.ResponseReceiverDisconnected += (x, y) =>
                    {
                        aResponseReceiverChannel1Disconnected = true;
                    };

                StringRequestReceivedEventArgs aReceivedMessage2 = null;
                aStringMessageReceiver2.RequestReceived += (x, y) =>
                    {
                        aReceivedMessage2 = y;
                    };
                bool aResponseReceiverChannel2Disconnected = false;
                aStringMessageReceiver2.ResponseReceiverDisconnected += (x, y) =>
                    {
                        aResponseReceiverChannel2Disconnected = true;
                    };


                aStringMessageSender1.SendMessage("Message1");
                aStringMessageSender2.SendMessage("Message2");

                string anAssociatedId1 = myDuplexChannelUnwrapper.GetAssociatedResponseReceiverId(aReceivedMessage1.ResponseReceiverId);
                string anAssociatedId2 = myDuplexChannelUnwrapper.GetAssociatedResponseReceiverId(aReceivedMessage2.ResponseReceiverId);

                Assert.AreEqual(anAssociatedId1, anAssociatedId2);

                myDuplexChannelUnwrapper.AttachedDuplexInputChannel.DisconnectResponseReceiver(anAssociatedId1);
                Assert.IsTrue(aResponseReceiverChannel1Disconnected);
                Assert.IsTrue(aResponseReceiverChannel2Disconnected);
            }
            finally
            {
                myDuplexChannelUnwrapper.DetachDuplexInputChannel();
                myDuplexChannelWrapper.DetachDuplexOutputChannel();
            }
        }


        private IConnectionProvider myConnectionProviderForWrapper;
        private IConnectionProvider myConnectionProviderForUnwrapper;

        private IDuplexChannelWrapper myDuplexChannelWrapper;
        private IDuplexChannelUnwrapper myDuplexChannelUnwrapper;

        private IDuplexOutputChannel myDuplexGlobalOutputChannel;
        private IDuplexInputChannel myDuplexGlobalInputChannel;
    }
}
