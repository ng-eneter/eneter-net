using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.Nodes.Router;
using Eneter.Messaging.EndPoints.StringMessages;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;



namespace Eneter.MessagingUnitTests.Nodes.Router
{
    [TestFixture]
    public class Test_Router
    {
        [Test]
        public void Route()
        {
            // Create channels
            string aChannel1Id = "Channel1Id";
            string aChannel2Id = "Channel2Id";
            string aChannel3Id = "Channel3Id";
            string aChannel4Id = "Channel4Id";

            IMessagingSystemFactory aDirectMessagingSystemFactory = new SynchronousMessagingSystemFactory();
            
            IInputChannel aReadingChannel1 = aDirectMessagingSystemFactory.CreateInputChannel(aChannel1Id);
            IOutputChannel aWritingChannel1 = aDirectMessagingSystemFactory.CreateOutputChannel(aChannel1Id);
            
            IInputChannel aReadingChannel2 = aDirectMessagingSystemFactory.CreateInputChannel(aChannel2Id);
            IOutputChannel aWritingChannel2 = aDirectMessagingSystemFactory.CreateOutputChannel(aChannel2Id);
            
            IOutputChannel aWritingChannel3 = aDirectMessagingSystemFactory.CreateOutputChannel(aChannel3Id);
            IInputChannel aReadingChannel3 = aDirectMessagingSystemFactory.CreateInputChannel(aChannel3Id);
            
            IOutputChannel aWritingChannel4 = aDirectMessagingSystemFactory.CreateOutputChannel(aChannel4Id);
            IInputChannel aReadingChannel4 = aDirectMessagingSystemFactory.CreateInputChannel(aChannel4Id);


            // Create router
            IRouterFactory aRouterFactory = new RouterFactory();
            IRouter aRouter = aRouterFactory.CreateRouter();

            // Configure router
            aRouter.AttachInputChannel(aReadingChannel1);
            aRouter.AttachInputChannel(aReadingChannel2);
            aRouter.AttachOutputChannel(aWritingChannel3);
            aRouter.AttachOutputChannel(aWritingChannel4);

            aRouter.AddConnection(aReadingChannel1.ChannelId, aWritingChannel3.ChannelId);
            aRouter.AddConnection(aReadingChannel1.ChannelId, aWritingChannel4.ChannelId);

            aRouter.AddConnection(aReadingChannel2.ChannelId, aWritingChannel4.ChannelId);

            // Create String senders and receivers
            IStringMessagesFactory aStringMessagesFactory = new StringMessagesFactory();
            IStringMessageSender aStringMessageSender1 = aStringMessagesFactory.CreateStringMessageSender();
            aStringMessageSender1.AttachOutputChannel(aWritingChannel1);

            IStringMessageSender aStringMessageSender2 = aStringMessagesFactory.CreateStringMessageSender();
            aStringMessageSender2.AttachOutputChannel(aWritingChannel2);

            IStringMessageReceiver aStringReceiver3 = aStringMessagesFactory.CreateStringMessageReceiver();
            aStringReceiver3.AttachInputChannel(aReadingChannel3);

            IStringMessageReceiver aStringReceiver4 = aStringMessagesFactory.CreateStringMessageReceiver();
            aStringReceiver4.AttachInputChannel(aReadingChannel4);


            // Observing string message receivers
            string aReceivedMessage3 = "";
            aStringReceiver3.MessageReceived += delegate(object sender, StringMessageEventArgs message)
            {
                aReceivedMessage3 = message.Message;
            };

            string aReceivedMessage4 = "";
            aStringReceiver4.MessageReceived += delegate(object sender, StringMessageEventArgs message)
            {
                aReceivedMessage4 = message.Message;
            };

            // Send the fist message
            string aMessage1 = "Message1";
            aStringMessageSender1.SendMessage(aMessage1);

            // Check
            Assert.AreEqual(aMessage1, aReceivedMessage3);
            Assert.AreEqual(aMessage1, aReceivedMessage4);

            // Send the second message
            aReceivedMessage3 = "";
            aReceivedMessage4 = "";

            string aMessage2 = "Message2";
            aStringMessageSender2.SendMessage(aMessage2);

            // Check
            Assert.AreEqual("", aReceivedMessage3);
            Assert.AreEqual(aMessage2, aReceivedMessage4);
        }
    }
}
