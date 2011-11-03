using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;
using Eneter.Messaging.Nodes.Broker;
using Eneter.Messaging.Diagnostic;

namespace Eneter.MessagingUnitTests.Nodes.Broker
{
    [TestFixture]
    public class Test_Broker
    {
        [Test]
        public void NotifySubscribers()
        {
            // Create channels
            IMessagingSystemFactory aMessagingSystem = new SynchronousMessagingSystemFactory();
            
            IDuplexInputChannel aBrokerInputChannel = aMessagingSystem.CreateDuplexInputChannel("BrokerChannel");
            IDuplexOutputChannel aSubscriber1ClientOutputChannel = aMessagingSystem.CreateDuplexOutputChannel("BrokerChannel");
            IDuplexOutputChannel aSubscriber2ClientOutputChannel = aMessagingSystem.CreateDuplexOutputChannel("BrokerChannel");
            IDuplexOutputChannel aSubscriber3ClientOutputChannel = aMessagingSystem.CreateDuplexOutputChannel("BrokerChannel");
            IDuplexOutputChannel aSubscriber4ClientOutputChannel = aMessagingSystem.CreateDuplexOutputChannel("BrokerChannel");

            IDuplexBrokerFactory aBrokerFactory = new DuplexBrokerFactory();

            IDuplexBroker aBroker = aBrokerFactory.CreateBroker();
            aBroker.AttachDuplexInputChannel(aBrokerInputChannel);
            
            IDuplexBrokerClient aBrokerClient1 = aBrokerFactory.CreateBrokerClient();
            BrokerMessageReceivedEventArgs aClient1ReceivedMessage = null;
            aBrokerClient1.BrokerMessageReceived += (x, y) =>
                {
                    aClient1ReceivedMessage = y;
                };
            aBrokerClient1.AttachDuplexOutputChannel(aSubscriber1ClientOutputChannel);

            IDuplexBrokerClient aBrokerClient2 = aBrokerFactory.CreateBrokerClient();
            BrokerMessageReceivedEventArgs aClient2ReceivedMessage = null;
            aBrokerClient2.BrokerMessageReceived += (x, y) =>
                {
                    aClient2ReceivedMessage = y;
                };
            aBrokerClient2.AttachDuplexOutputChannel(aSubscriber2ClientOutputChannel);

            IDuplexBrokerClient aBrokerClient3 = aBrokerFactory.CreateBrokerClient();
            BrokerMessageReceivedEventArgs aClient3ReceivedMessage = null;
            aBrokerClient3.BrokerMessageReceived += (x, y) =>
            {
                aClient3ReceivedMessage = y;
            };
            aBrokerClient3.AttachDuplexOutputChannel(aSubscriber3ClientOutputChannel);

            IDuplexBrokerClient aBrokerClient4 = aBrokerFactory.CreateBrokerClient();
            BrokerMessageReceivedEventArgs aClient4ReceivedMessage = null;
            aBrokerClient4.BrokerMessageReceived += (x, y) =>
            {
                aClient4ReceivedMessage = y;
            };
            aBrokerClient4.AttachDuplexOutputChannel(aSubscriber4ClientOutputChannel);

            string[] aSubscription1 = {"TypeA", "TypeB"};
            aBrokerClient1.Subscribe(aSubscription1);

            string[] aSubscription2 = { "TypeA" };
            aBrokerClient2.Subscribe(aSubscription2);

            string[] aSubscription3 = { "MTypeC" };
            aBrokerClient3.Subscribe(aSubscription3);

            // Subscription using the regular expression.
            // Note: Subscribe for all message types starting with the character 'T'. 
            string[] aSubscription4 = { "^T" };
            aBrokerClient4.SubscribeRegExp(aSubscription4);


            aBrokerClient3.SendMessage("TypeA", "Message A");
            Assert.AreEqual("TypeA", aClient1ReceivedMessage.MessageTypeId);
            Assert.AreEqual("Message A", (string)aClient1ReceivedMessage.Message);
            Assert.AreEqual(null, aClient1ReceivedMessage.ReceivingError);
            
            Assert.AreEqual("TypeA", aClient2ReceivedMessage.MessageTypeId);
            Assert.AreEqual("Message A", (string)aClient2ReceivedMessage.Message);
            Assert.AreEqual(null, aClient2ReceivedMessage.ReceivingError);

            Assert.AreEqual(null, aClient3ReceivedMessage);

            Assert.AreEqual("TypeA", aClient4ReceivedMessage.MessageTypeId);
            Assert.AreEqual("Message A", (string)aClient4ReceivedMessage.Message);
            Assert.AreEqual(null, aClient4ReceivedMessage.ReceivingError);

            aClient1ReceivedMessage = null;
            aClient2ReceivedMessage = null;
            aClient3ReceivedMessage = null;
            aClient4ReceivedMessage = null;

            aBrokerClient2.Unsubscribe();
            
            aBrokerClient3.SendMessage("MTypeC", "Message MTC");

            Assert.AreEqual(null, aClient1ReceivedMessage);

            Assert.AreEqual(null, aClient2ReceivedMessage);

            Assert.AreEqual("MTypeC", aClient3ReceivedMessage.MessageTypeId);
            Assert.AreEqual("Message MTC", (string)aClient3ReceivedMessage.Message);
            Assert.AreEqual(null, aClient3ReceivedMessage.ReceivingError);

            Assert.AreEqual(null, aClient4ReceivedMessage);

            
            aClient1ReceivedMessage = null;
            aClient2ReceivedMessage = null;
            aClient3ReceivedMessage = null;
            aClient4ReceivedMessage = null;

            aBrokerClient3.SendMessage("TypeA", "Message A");
            Assert.AreEqual("TypeA", aClient1ReceivedMessage.MessageTypeId);
            Assert.AreEqual("Message A", (string)aClient1ReceivedMessage.Message);
            Assert.AreEqual(null, aClient1ReceivedMessage.ReceivingError);

            Assert.AreEqual(null, aClient2ReceivedMessage);

            Assert.AreEqual(null, aClient3ReceivedMessage);

            Assert.AreEqual("TypeA", aClient4ReceivedMessage.MessageTypeId);
            Assert.AreEqual("Message A", (string)aClient4ReceivedMessage.Message);
            Assert.AreEqual(null, aClient4ReceivedMessage.ReceivingError);


            aClient1ReceivedMessage = null;
            aClient2ReceivedMessage = null;
            aClient3ReceivedMessage = null;
            aClient4ReceivedMessage = null;


            string[] aNewMessageType = { "TypeA" };
            aBrokerClient3.Subscribe(aNewMessageType);
            
            aBrokerClient1.DetachDuplexOutputChannel();

            aBrokerClient3.SendMessage("TypeA", "Message A");
            Assert.AreEqual(null, aClient1ReceivedMessage);

            Assert.AreEqual(null, aClient2ReceivedMessage);

            Assert.AreEqual("TypeA", aClient3ReceivedMessage.MessageTypeId);
            Assert.AreEqual("Message A", (string)aClient3ReceivedMessage.Message);
            Assert.AreEqual(null, aClient3ReceivedMessage.ReceivingError);

        }
    }
}
