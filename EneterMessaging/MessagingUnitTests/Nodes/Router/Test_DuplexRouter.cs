using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.Nodes.Router;
using Eneter.Messaging.EndPoints.StringMessages;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;

namespace Eneter.MessagingUnitTests.Nodes.Router
{
    [TestFixture]
    public class Test_DuplexRouter
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
            IDuplexRouterFactory aDuplexDispatcherFactory = new DuplexRouterFactory(myMessagingSystemFactory);
            myDuplexRouter = aDuplexDispatcherFactory.CreateDuplexRouter();
        }

        [Test]
        public void SendAndReceive()
        {
            myDuplexRouter.AttachDuplexInputChannel(myMessagingSystemFactory.CreateDuplexInputChannel("ChannelA_1"));
            myDuplexRouter.AttachDuplexInputChannel(myMessagingSystemFactory.CreateDuplexInputChannel("ChannelA_2"));
            myDuplexRouter.AttachDuplexInputChannel(myMessagingSystemFactory.CreateDuplexInputChannel("ChannelA_3"));

            myStringMessageSender11.AttachDuplexOutputChannel(myMessagingSystemFactory.CreateDuplexOutputChannel("ChannelA_1"));
            myStringMessageSender12.AttachDuplexOutputChannel(myMessagingSystemFactory.CreateDuplexOutputChannel("ChannelA_2"));
            myStringMessageSender13.AttachDuplexOutputChannel(myMessagingSystemFactory.CreateDuplexOutputChannel("ChannelA_3"));

            myStringMessageSender22.AttachDuplexOutputChannel(myMessagingSystemFactory.CreateDuplexOutputChannel("ChannelA_2"));

            myStringMessageReceiver1.AttachDuplexInputChannel(myMessagingSystemFactory.CreateDuplexInputChannel("ChannelB_1"));
            myStringMessageReceiver2.AttachDuplexInputChannel(myMessagingSystemFactory.CreateDuplexInputChannel("ChannelB_2"));
            myStringMessageReceiver3.AttachDuplexInputChannel(myMessagingSystemFactory.CreateDuplexInputChannel("ChannelB_3"));

            myDuplexRouter.AddConnection("ChannelA_1", "ChannelB_1");
            myDuplexRouter.AddConnection("ChannelA_1", "ChannelB_3");

            myDuplexRouter.AddConnection("ChannelA_2", "ChannelB_2");
            
            myDuplexRouter.AddConnection("ChannelA_3", "ChannelB_1");
            myDuplexRouter.AddConnection("ChannelA_3", "ChannelB_2");
            myDuplexRouter.AddConnection("ChannelA_3", "ChannelB_3");

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
            Assert.AreEqual("", aReceivedMessage2);
            Assert.AreEqual("Message1", aReceivedMessage3);

            Assert.AreEqual("Response1Response3", aReceivedResponse11);
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

            Assert.AreEqual("", aReceivedMessage1);
            Assert.AreEqual("Message2", aReceivedMessage2);
            Assert.AreEqual("", aReceivedMessage3);

            Assert.AreEqual("", aReceivedResponse11);
            Assert.AreEqual("Response2", aReceivedResponse12);
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

            Assert.AreEqual("", aReceivedMessage1);
            Assert.AreEqual("Message22", aReceivedMessage2);
            Assert.AreEqual("", aReceivedMessage3);

            Assert.AreEqual("", aReceivedResponse11);
            Assert.AreEqual("", aReceivedResponse12);
            Assert.AreEqual("", aReceivedResponse13);
            Assert.AreEqual("Response2", aReceivedResponse22);


            aReceivedMessage1 = "";
            aReceivedMessage2 = "";
            aReceivedMessage3 = "";
            aReceivedResponse11 = "";
            aReceivedResponse12 = "";
            aReceivedResponse13 = "";
            aReceivedResponse22 = "";
            myDuplexRouter.RemoveConnection("ChannelA_3", "ChannelB_3");
            myStringMessageSender13.SendMessage("Message3");

            Assert.AreEqual("Message3", aReceivedMessage1);
            Assert.AreEqual("Message3", aReceivedMessage2);
            Assert.AreEqual("", aReceivedMessage3);

            Assert.AreEqual("", aReceivedResponse11);
            Assert.AreEqual("", aReceivedResponse12);
            Assert.AreEqual("Response1Response2", aReceivedResponse13);
            Assert.AreEqual("", aReceivedResponse22);


            aReceivedMessage1 = "";
            aReceivedMessage2 = "";
            aReceivedMessage3 = "";
            aReceivedResponse11 = "";
            aReceivedResponse12 = "";
            aReceivedResponse13 = "";
            aReceivedResponse22 = "";
            myDuplexRouter.RemoveAllConnections();
            myStringMessageSender12.SendMessage("Message2");

            Assert.AreEqual("", aReceivedMessage1);
            Assert.AreEqual("", aReceivedMessage2);
            Assert.AreEqual("", aReceivedMessage3);

            Assert.AreEqual("", aReceivedResponse11);
            Assert.AreEqual("", aReceivedResponse12);
            Assert.AreEqual("", aReceivedResponse13);
            Assert.AreEqual("", aReceivedResponse22);
        }


        private IMessagingSystemFactory myMessagingSystemFactory;
        private IDuplexRouter myDuplexRouter;

        private IDuplexStringMessageSender myStringMessageSender11;
        private IDuplexStringMessageSender myStringMessageSender12;
        private IDuplexStringMessageSender myStringMessageSender13;

        private IDuplexStringMessageSender myStringMessageSender22;

        private IDuplexStringMessageReceiver myStringMessageReceiver1;
        private IDuplexStringMessageReceiver myStringMessageReceiver2;
        private IDuplexStringMessageReceiver myStringMessageReceiver3;
    }
}
