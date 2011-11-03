using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.Nodes.Dispatcher;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;

namespace Eneter.MessagingUnitTests.Nodes.Dispatcher
{
    [TestFixture]
    public class Test_Dispatcher
    {
        [TestFixtureSetUp]
        public void Setup()
        {
            // Create channels
            IMessagingSystemFactory aMessagingSystemFactory = new SynchronousMessagingSystemFactory();
            myWritingChannel1 = aMessagingSystemFactory.CreateOutputChannel("Channel1");
            myReadingChannel1 = aMessagingSystemFactory.CreateInputChannel("Channel1");

            myWritingChannel2 = aMessagingSystemFactory.CreateOutputChannel("Channel2");
            myReadingChannel2 = aMessagingSystemFactory.CreateInputChannel("Channel2");

            myWritingChannel3 = aMessagingSystemFactory.CreateOutputChannel("Channel3");
            myReadingChannel3 = aMessagingSystemFactory.CreateInputChannel("Channel3");

            myWritingChannel4 = aMessagingSystemFactory.CreateOutputChannel("Channel4");
            myReadingChannel4 = aMessagingSystemFactory.CreateInputChannel("Channel4");

            // Create dispatcher
            IDispatcherFactory aDispatcherFactory = new DispatcherFactory();
            myDispatcher = aDispatcherFactory.CreateDispatcher();

            // Attach input channels
            myDispatcher.AttachInputChannel(myReadingChannel1);
            myDispatcher.AttachInputChannel(myReadingChannel2);

            // Attach ouptut channels
            myDispatcher.AttachOutputChannel(myWritingChannel3);
            myDispatcher.AttachOutputChannel(myWritingChannel4);
        }

        [Test]
        public void Dispatching()
        {
            // Listen output from dispatcher
            ChannelMessageEventArgs aReceivedFromChannel3 = null;
            myReadingChannel3.MessageReceived += (x, y) =>
                {
                    aReceivedFromChannel3 = y;
                };
            myReadingChannel3.StartListening();

            ChannelMessageEventArgs aReceivedFromChannel4 = null;
            myReadingChannel4.MessageReceived += (x, y) =>
                {
                    aReceivedFromChannel4 = y;
                };
            myReadingChannel4.StartListening();

            // Send message to dispatcher via channel1
            myWritingChannel1.SendMessage("MyMessageA");

            // Check
            Assert.AreEqual("MyMessageA", aReceivedFromChannel3.Message);
            Assert.AreEqual("MyMessageA", aReceivedFromChannel4.Message);

            // Send message to dispatcher via channel2
            aReceivedFromChannel3 = null;
            aReceivedFromChannel4 = null;
            myWritingChannel2.SendMessage("MyMessageB");

            // Check
            Assert.AreEqual("MyMessageB", aReceivedFromChannel3.Message);
            Assert.AreEqual("MyMessageB", aReceivedFromChannel4.Message);
        }

        private IDispatcher myDispatcher;

        private IOutputChannel myWritingChannel1;
        private IInputChannel myReadingChannel1;

        private IOutputChannel myWritingChannel2;
        private IInputChannel myReadingChannel2;

        private IOutputChannel myWritingChannel3;
        private IInputChannel myReadingChannel3;

        private IOutputChannel myWritingChannel4;
        private IInputChannel myReadingChannel4;

    }
}
