using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.Nodes.Dispatcher;
using Eneter.Messaging.EndPoints.StringMessages;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;
using Eneter.Messaging.Infrastructure.ConnectionProvider;

namespace Eneter.MessagingUnitTests.Nodes.Dispatcher
{
    [TestFixture]
    public class Test_DuplexDispatcher
    {
        [SetUp]
        public void Setup()
        {
            IDuplexStringMessagesFactory aDuplexStringMessagesFactory = new DuplexStringMessagesFactory();
            myStringMessageSender11 = aDuplexStringMessagesFactory.CreateDuplexStringMessageSender();
            myStringMessageSender12 = aDuplexStringMessagesFactory.CreateDuplexStringMessageSender();
            myStringMessageSender13 = aDuplexStringMessagesFactory.CreateDuplexStringMessageSender();
            myStringMessageSender22 = aDuplexStringMessagesFactory.CreateDuplexStringMessageSender();
            myStringMessageReceiver1 = aDuplexStringMessagesFactory.CreateDuplexStringMessageReceiver();
            myStringMessageReceiver2 = aDuplexStringMessagesFactory.CreateDuplexStringMessageReceiver();
            myStringMessageReceiver3 = aDuplexStringMessagesFactory.CreateDuplexStringMessageReceiver();


            myMessagingSystemFactory = new SynchronousMessagingSystemFactory();
            IDuplexDispatcherFactory aDuplexDispatcherFactory = new DuplexDispatcherFactory(myMessagingSystemFactory);
            myDuplexDispatcher = aDuplexDispatcherFactory.CreateDuplexDispatcher();
        }

        [Test]
        public void SendAndReceive()
        {
            IConnectionProviderFactory aConnectionProviderFactory = new ConnectionProviderFactory();
            IConnectionProvider aConnectionProvider = aConnectionProviderFactory.CreateConnectionProvider(myMessagingSystemFactory);

            aConnectionProvider.Attach(myDuplexDispatcher, "ChannelA_1");
            aConnectionProvider.Attach(myDuplexDispatcher, "ChannelA_2");
            aConnectionProvider.Attach(myDuplexDispatcher, "ChannelA_3");

            aConnectionProvider.Attach(myStringMessageSender11, "ChannelA_1");
            aConnectionProvider.Attach(myStringMessageSender12, "ChannelA_2");
            aConnectionProvider.Attach(myStringMessageSender13, "ChannelA_3");
            
            aConnectionProvider.Attach(myStringMessageSender22, "ChannelA_2");

            aConnectionProvider.Attach(myStringMessageReceiver1, "ChannelB_1");
            aConnectionProvider.Attach(myStringMessageReceiver2, "ChannelB_2");
            aConnectionProvider.Attach(myStringMessageReceiver3, "ChannelB_3");

            myDuplexDispatcher.AddDuplexOutputChannel("ChannelB_1");
            myDuplexDispatcher.AddDuplexOutputChannel("ChannelB_2");
            myDuplexDispatcher.AddDuplexOutputChannel("ChannelB_3");

            string aReceivedMessage1 = "";
            myStringMessageReceiver1.RequestReceived += (x, y) =>
                {
                    aReceivedMessage1 = y.RequestMessage;
                    myStringMessageReceiver1.SendResponseMessage(y.ResponseReceiverId, "Response1");
                };

            string aReceivedMessage2 = "";
            myStringMessageReceiver2.RequestReceived += (x, y) =>
            {
                aReceivedMessage2 = y.RequestMessage;
                myStringMessageReceiver2.SendResponseMessage(y.ResponseReceiverId, "Response2");
            };

            string aReceivedMessage3 = "";
            myStringMessageReceiver3.RequestReceived += (x, y) =>
            {
                aReceivedMessage3 = y.RequestMessage;
                myStringMessageReceiver3.SendResponseMessage(y.ResponseReceiverId, "Response3");
            };

            string aReceivedResponse11 = "";
            myStringMessageSender11.ResponseReceived += (x, y) =>
                {
                    aReceivedResponse11 += y.ResponseMessage;
                };

            string aReceivedResponse12 = "";
            myStringMessageSender12.ResponseReceived += (x, y) =>
            {
                aReceivedResponse12 += y.ResponseMessage;
            };

            string aReceivedResponse13 = "";
            myStringMessageSender13.ResponseReceived += (x, y) =>
            {
                aReceivedResponse13 += y.ResponseMessage;
            };

            string aReceivedResponse22 = "";
            myStringMessageSender22.ResponseReceived += (x, y) =>
            {
                aReceivedResponse22 += y.ResponseMessage;
            };



            myStringMessageSender11.SendMessage("Message1");
            
            Assert.AreEqual("Message1", aReceivedMessage1);
            Assert.AreEqual("Message1", aReceivedMessage2);
            Assert.AreEqual("Message1", aReceivedMessage3);

            Assert.AreEqual("Response1Response2Response3", aReceivedResponse11);
            Assert.AreEqual("", aReceivedResponse12);
            Assert.AreEqual("", aReceivedResponse13);
            Assert.AreEqual("", aReceivedResponse22);


            aReceivedMessage1 = "";
            aReceivedMessage2 = "";
            aReceivedMessage3 = "";
            aReceivedResponse11 = "";
            aReceivedResponse12 = "";
            aReceivedResponse13 = "";
            aReceivedResponse22 = "";
            myStringMessageSender12.SendMessage("Message2");

            Assert.AreEqual("Message2", aReceivedMessage1);
            Assert.AreEqual("Message2", aReceivedMessage2);
            Assert.AreEqual("Message2", aReceivedMessage3);

            Assert.AreEqual("", aReceivedResponse11);
            Assert.AreEqual("Response1Response2Response3", aReceivedResponse12);
            Assert.AreEqual("", aReceivedResponse13);
            Assert.AreEqual("", aReceivedResponse22);


            aReceivedMessage1 = "";
            aReceivedMessage2 = "";
            aReceivedMessage3 = "";
            aReceivedResponse11 = "";
            aReceivedResponse12 = "";
            aReceivedResponse13 = "";
            aReceivedResponse22 = "";
            myStringMessageSender22.SendMessage("Message22");

            Assert.AreEqual("Message22", aReceivedMessage1);
            Assert.AreEqual("Message22", aReceivedMessage2);
            Assert.AreEqual("Message22", aReceivedMessage3);

            Assert.AreEqual("", aReceivedResponse11);
            Assert.AreEqual("", aReceivedResponse12);
            Assert.AreEqual("", aReceivedResponse13);
            Assert.AreEqual("Response1Response2Response3", aReceivedResponse22);


            aReceivedMessage1 = "";
            aReceivedMessage2 = "";
            aReceivedMessage3 = "";
            aReceivedResponse11 = "";
            aReceivedResponse12 = "";
            aReceivedResponse13 = "";
            aReceivedResponse22 = "";
            myDuplexDispatcher.RemoveDuplexOutputChannel("ChannelB_2");
            myStringMessageSender12.SendMessage("Message2");

            Assert.AreEqual("Message2", aReceivedMessage1);
            Assert.AreEqual("", aReceivedMessage2);
            Assert.AreEqual("Message2", aReceivedMessage3);

            Assert.AreEqual("", aReceivedResponse11);
            Assert.AreEqual("Response1Response3", aReceivedResponse12);
            Assert.AreEqual("", aReceivedResponse13);
            Assert.AreEqual("", aReceivedResponse22);


            aReceivedMessage1 = "";
            aReceivedMessage2 = "";
            aReceivedMessage3 = "";
            aReceivedResponse11 = "";
            aReceivedResponse12 = "";
            aReceivedResponse13 = "";
            aReceivedResponse22 = "";
            myDuplexDispatcher.RemoveDuplexOutputChannel("ChannelB_2");
            myStringMessageSender12.SendMessage("Message2");

            Assert.AreEqual("Message2", aReceivedMessage1);
            Assert.AreEqual("", aReceivedMessage2);
            Assert.AreEqual("Message2", aReceivedMessage3);

            Assert.AreEqual("", aReceivedResponse11);
            Assert.AreEqual("Response1Response3", aReceivedResponse12);
            Assert.AreEqual("", aReceivedResponse13);
            Assert.AreEqual("", aReceivedResponse22);


            aReceivedMessage1 = "";
            aReceivedMessage2 = "";
            aReceivedMessage3 = "";
            aReceivedResponse11 = "";
            aReceivedResponse12 = "";
            aReceivedResponse13 = "";
            aReceivedResponse22 = "";
            myDuplexDispatcher.RemoveAllDuplexOutputChannels();
            myStringMessageSender12.SendMessage("Message2");

            Assert.AreEqual("", aReceivedMessage1);
            Assert.AreEqual("", aReceivedMessage2);
            Assert.AreEqual("", aReceivedMessage3);

            Assert.AreEqual("", aReceivedResponse11);
            Assert.AreEqual("", aReceivedResponse12);
            Assert.AreEqual("", aReceivedResponse13);
            Assert.AreEqual("", aReceivedResponse22);
        }

        //[Test]
        public void GetAssociatedResponseReceiverId()
        {
            IConnectionProviderFactory aConnectionProviderFactory = new ConnectionProviderFactory();
            IConnectionProvider aConnectionProvider = aConnectionProviderFactory.CreateConnectionProvider(myMessagingSystemFactory);

            aConnectionProvider.Attach(myDuplexDispatcher, "ChannelA_1");
            aConnectionProvider.Attach(myDuplexDispatcher, "ChannelA_2");

            aConnectionProvider.Attach(myStringMessageSender11, "ChannelA_1");
            aConnectionProvider.Attach(myStringMessageSender12, "ChannelA_2");

            aConnectionProvider.Attach(myStringMessageReceiver1, "ChannelB_1");
            aConnectionProvider.Attach(myStringMessageReceiver2, "ChannelB_2");

            myDuplexDispatcher.AddDuplexOutputChannel("ChannelB_1");
            myDuplexDispatcher.AddDuplexOutputChannel("ChannelB_2");

            string aResponseReceiverId1 = "";
            myStringMessageReceiver1.RequestReceived += (x, y) =>
            {
                aResponseReceiverId1 = y.ResponseReceiverId;
            };

            string aResponseReceiverId2 = "";
            myStringMessageReceiver2.RequestReceived += (x, y) =>
            {
                aResponseReceiverId2 = y.ResponseReceiverId;
            };


            myStringMessageSender11.SendMessage("Message1");
            string aClientId1FromReceiver1 = myDuplexDispatcher.GetAssociatedResponseReceiverId(aResponseReceiverId1);
            string aClientId1FromReceiver2 = myDuplexDispatcher.GetAssociatedResponseReceiverId(aResponseReceiverId2);
            Assert.AreEqual(myStringMessageSender11.AttachedDuplexOutputChannel.ResponseReceiverId, aClientId1FromReceiver1);
            Assert.AreEqual(myStringMessageSender11.AttachedDuplexOutputChannel.ResponseReceiverId, aClientId1FromReceiver2);

            myStringMessageSender12.SendMessage("Message2");
            string aClientId2FromReceiver1 = myDuplexDispatcher.GetAssociatedResponseReceiverId(aResponseReceiverId1);
            string aClientId2FromReceiver2 = myDuplexDispatcher.GetAssociatedResponseReceiverId(aResponseReceiverId2);
            Assert.AreEqual(myStringMessageSender12.AttachedDuplexOutputChannel.ResponseReceiverId, aClientId2FromReceiver1);
            Assert.AreEqual(myStringMessageSender12.AttachedDuplexOutputChannel.ResponseReceiverId, aClientId2FromReceiver2);
        }


        private IMessagingSystemFactory myMessagingSystemFactory;
        private IDuplexDispatcher myDuplexDispatcher;
        
        private IDuplexStringMessageSender myStringMessageSender11;
        private IDuplexStringMessageSender myStringMessageSender12;
        private IDuplexStringMessageSender myStringMessageSender13;
        
        private IDuplexStringMessageSender myStringMessageSender22;

        private IDuplexStringMessageReceiver myStringMessageReceiver1;
        private IDuplexStringMessageReceiver myStringMessageReceiver2;
        private IDuplexStringMessageReceiver myStringMessageReceiver3;
    }
}
