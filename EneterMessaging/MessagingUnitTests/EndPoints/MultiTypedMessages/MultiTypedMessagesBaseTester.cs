using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.EndPoints.TypedMessages;
using NUnit.Framework;
using System.Threading;
using Eneter.Messaging.DataProcessing.Serializing;

namespace Eneter.MessagingUnitTests.EndPoints.MultiTypedMessages
{
    public abstract class MultiTypedMessagesBaseTester
    {
        [Serializable]
        public class CustomClass
        {
            public string Name { get; set; }
            public int Count { get; set; }
        }

        protected void Setup(IMessagingSystemFactory messagingSystemFactory, string channelId, ISerializer serializer)
        {
            MessagingSystemFactory = messagingSystemFactory;

            DuplexOutputChannel = MessagingSystemFactory.CreateDuplexOutputChannel(channelId);
            DuplexInputChannel = MessagingSystemFactory.CreateDuplexInputChannel(channelId);

            IMultiTypedMessagesFactory aMessageFactory = new MultiTypedMessagesFactory()
            {
                Serializer = serializer
            };
            Requester = aMessageFactory.CreateMultiTypedMessageSender();
            Responser = aMessageFactory.CreateMultiTypedMessageReceiver();
        }

        [Test]
        public void SendReceive_Message()
        {
            // The test can be performed from more threads therefore we must synchronize.
            AutoResetEvent aMessageReceivedEvent = new AutoResetEvent(false);

            int aReceivedMessage1 = 0;
            Responser.RegisterRequestMessageReceiver<int>((x, y) =>
                {
                    aReceivedMessage1 = y.RequestMessage;

                    // Send the response
                    Responser.SendResponseMessage<string>(y.ResponseReceiverId, "hello");
                });

            CustomClass aReceivedMessage2 = null;
            Responser.RegisterRequestMessageReceiver<CustomClass>((x, y) =>
                {
                    aReceivedMessage2 = y.RequestMessage;

                    // Send the response
                    CustomClass aResponse = new CustomClass();
                    aResponse.Name = "Car";
                    aResponse.Count = 100;

                    Responser.SendResponseMessage<CustomClass>(y.ResponseReceiverId, aResponse);
                });


            Responser.AttachDuplexInputChannel(DuplexInputChannel);

            string aReceivedResponse1 = "";
            Requester.RegisterResponseMessageReceiver<string>((x, y) =>
                {
                    aReceivedResponse1 = y.ResponseMessage;
                });

            CustomClass aReceivedResponse2 = null;
            Requester.RegisterResponseMessageReceiver<CustomClass>((x, y) =>
                {
                    aReceivedResponse2 = y.ResponseMessage;

                    // Signal that the response message was received -> the loop is closed.
                    aMessageReceivedEvent.Set();
                });
            Requester.AttachDuplexOutputChannel(DuplexOutputChannel);

            try
            {
                Requester.SendRequestMessage<int>(1000);

                CustomClass aCustomRequest = new CustomClass();
                aCustomRequest.Name = "House";
                aCustomRequest.Count = 1000;
                Requester.SendRequestMessage<CustomClass>(aCustomRequest);

                // Wait for the signal that the message is received.
                aMessageReceivedEvent.WaitOne();
                //Assert.IsTrue(aMessageReceivedEvent.WaitOne(2000));
            }
            finally
            {
                Requester.DetachDuplexOutputChannel();
                Responser.DetachDuplexInputChannel();
                Responser.DetachDuplexInputChannel();
            }

            // Check received values
            Assert.AreEqual(1000, aReceivedMessage1);
            Assert.AreEqual("hello", aReceivedResponse1);

            Assert.IsNotNull(aReceivedMessage2);
            Assert.AreEqual("House", aReceivedMessage2.Name);
            Assert.AreEqual(1000, aReceivedMessage2.Count);

            Assert.IsNotNull(aReceivedResponse2);
            Assert.AreEqual("Car", aReceivedResponse2.Name);
            Assert.AreEqual(100, aReceivedResponse2.Count);
        }

        [Test]
        public void SendReceive_Message_PerClientSerializer()
        {
            string aClient1Id = null;

            IMultiTypedMessagesFactory aSender1Factory = new MultiTypedMessagesFactory(new XmlStringSerializer());
            IMultiTypedMessagesFactory aSender2Factory = new MultiTypedMessagesFactory(new BinarySerializer());
            IMultiTypedMessagesFactory aReceiverFactory = new MultiTypedMessagesFactory()
            {
                SerializerProvider = x => (x == aClient1Id) ? (ISerializer)new XmlStringSerializer() : (ISerializer)new BinarySerializer()
            };

            IMultiTypedMessageSender aSender1 = aSender1Factory.CreateMultiTypedMessageSender();
            IMultiTypedMessageSender aSender2 = aSender2Factory.CreateMultiTypedMessageSender();
            IMultiTypedMessageReceiver aReceiver = aReceiverFactory.CreateMultiTypedMessageReceiver();
            aReceiver.ResponseReceiverConnected += (x, y) => aClient1Id = aClient1Id ?? y.ResponseReceiverId;

            int aReceivedMessage1 = 0;
            aReceiver.RegisterRequestMessageReceiver<int>((x, y) =>
            {
                aReceivedMessage1 = y.RequestMessage;

                // Send the response
                aReceiver.SendResponseMessage<string>(y.ResponseReceiverId, "hello");
            });

            CustomClass aReceivedMessage2 = null;
            aReceiver.RegisterRequestMessageReceiver<CustomClass>((x, y) =>
            {
                aReceivedMessage2 = y.RequestMessage;

                // Send the response
                CustomClass aResponse = new CustomClass();
                aResponse.Name = "Car";
                aResponse.Count = 100;

                aReceiver.SendResponseMessage<CustomClass>(y.ResponseReceiverId, aResponse);
            });

            aReceiver.AttachDuplexInputChannel(DuplexInputChannel);

            string aSender1ReceivedResponse1 = "";
            aSender1.RegisterResponseMessageReceiver<string>((x, y) =>
            {
                aSender1ReceivedResponse1 = y.ResponseMessage;
            });

            AutoResetEvent aSender1MessagesReceivedEvent = new AutoResetEvent(false);
            CustomClass aSender1ReceivedResponse2 = null;
            aSender1.RegisterResponseMessageReceiver<CustomClass>((x, y) =>
            {
                aSender1ReceivedResponse2 = y.ResponseMessage;

                // Signal that the response message was received -> the loop is closed.
                aSender1MessagesReceivedEvent.Set();
            });
            aSender1.AttachDuplexOutputChannel(MessagingSystemFactory.CreateDuplexOutputChannel(DuplexInputChannel.ChannelId));


            string aSender2ReceivedResponse1 = "";
            aSender2.RegisterResponseMessageReceiver<string>((x, y) =>
            {
                aSender2ReceivedResponse1 = y.ResponseMessage;
            });

            AutoResetEvent aSender2MessagesReceivedEvent = new AutoResetEvent(false);
            CustomClass aSender2ReceivedResponse2 = null;
            aSender2.RegisterResponseMessageReceiver<CustomClass>((x, y) =>
            {
                aSender2ReceivedResponse2 = y.ResponseMessage;

                // Signal that the response message was received -> the loop is closed.
                aSender2MessagesReceivedEvent.Set();
            });
            aSender2.AttachDuplexOutputChannel(MessagingSystemFactory.CreateDuplexOutputChannel(DuplexInputChannel.ChannelId));


            try
            {
                aSender1.SendRequestMessage<int>(1000);

                CustomClass aCustomRequest = new CustomClass();
                aCustomRequest.Name = "House";
                aCustomRequest.Count = 1000;
                aSender1.SendRequestMessage<CustomClass>(aCustomRequest);

                aSender2.SendRequestMessage<int>(1000);
                aSender2.SendRequestMessage<CustomClass>(aCustomRequest);

                // Wait for the signal that the message is received.
                aSender1MessagesReceivedEvent.WaitIfNotDebugging(2000);
                aSender2MessagesReceivedEvent.WaitIfNotDebugging(2000);
            }
            finally
            {
                aSender1.DetachDuplexOutputChannel();
                aSender2.DetachDuplexOutputChannel();
                aReceiver.DetachDuplexInputChannel();
            }

            // Check received values
            Assert.AreEqual(1000, aReceivedMessage1);
            Assert.AreEqual("hello", aSender1ReceivedResponse1);
            Assert.AreEqual("hello", aSender2ReceivedResponse1);

            Assert.IsNotNull(aReceivedMessage2);
            Assert.AreEqual("House", aReceivedMessage2.Name);
            Assert.AreEqual(1000, aReceivedMessage2.Count);

            Assert.IsNotNull(aSender1ReceivedResponse2);
            Assert.AreEqual("Car", aSender1ReceivedResponse2.Name);
            Assert.AreEqual(100, aSender1ReceivedResponse2.Count);

            Assert.IsNotNull(aSender2ReceivedResponse2);
            Assert.AreEqual("Car", aSender2ReceivedResponse2.Name);
            Assert.AreEqual(100, aSender2ReceivedResponse2.Count);
        }

        [Test]
        public void SendReceive_NullMessage()
        {
            // The test can be performed from more threads therefore we must synchronize.
            AutoResetEvent aMessageReceivedEvent = new AutoResetEvent(false);

            CustomClass aReceivedMessage2 = null;
            Responser.RegisterRequestMessageReceiver<CustomClass>((x, y) =>
            {
                aReceivedMessage2 = y.RequestMessage;

                Responser.SendResponseMessage<CustomClass>(y.ResponseReceiverId, null);
            });


            Responser.AttachDuplexInputChannel(DuplexInputChannel);

            CustomClass aReceivedResponse2 = new CustomClass();
            Requester.RegisterResponseMessageReceiver<CustomClass>((x, y) =>
            {
                aReceivedResponse2 = y.ResponseMessage;

                // Signal that the response message was received -> the loop is closed.
                aMessageReceivedEvent.Set();
            });
            Requester.AttachDuplexOutputChannel(DuplexOutputChannel);

            try
            {
                Requester.SendRequestMessage<int>(1000);

                Requester.SendRequestMessage<CustomClass>(null);

                // Wait for the signal that the message is received.
                aMessageReceivedEvent.WaitOne();
                //Assert.IsTrue(aMessageReceivedEvent.WaitOne(2000));
            }
            finally
            {
                Requester.DetachDuplexOutputChannel();
                Responser.DetachDuplexInputChannel();
            }

            // Check received values
            Assert.IsNull(aReceivedMessage2);
            Assert.IsNull(aReceivedResponse2);
        }

        [Test]
        public void RegisterUnregister()
        {
            // Registering / unregistering in service.
            Responser.RegisterRequestMessageReceiver<int>((x, y) => { });
            Responser.RegisterRequestMessageReceiver<CustomClass>((x, y) => { });
            Responser.RegisterRequestMessageReceiver<string>((x, y) => { });

            Assert.AreEqual(3, Responser.RegisteredRequestMessageTypes.Count());
            Assert.IsTrue(Responser.RegisteredRequestMessageTypes.Any(x => x == typeof(int)));
            Assert.IsTrue(Responser.RegisteredRequestMessageTypes.Any(x => x == typeof(CustomClass)));
            Assert.IsTrue(Responser.RegisteredRequestMessageTypes.Any(x => x == typeof(string)));

            Responser.UnregisterRequestMessageReceiver<CustomClass>();

            Assert.AreEqual(2, Responser.RegisteredRequestMessageTypes.Count());
            Assert.IsTrue(Responser.RegisteredRequestMessageTypes.Any(x => x == typeof(int)));
            Assert.IsTrue(Responser.RegisteredRequestMessageTypes.Any(x => x == typeof(string)));


            // Registering / unregistering in client.
            Requester.RegisterResponseMessageReceiver<int>((x, y) => { });
            Requester.RegisterResponseMessageReceiver<CustomClass>((x, y) => { });
            Requester.RegisterResponseMessageReceiver<string>((x, y) => { });

            Assert.AreEqual(3, Requester.RegisteredResponseMessageTypes.Count());
            Assert.IsTrue(Requester.RegisteredResponseMessageTypes.Any(x => x == typeof(int)));
            Assert.IsTrue(Requester.RegisteredResponseMessageTypes.Any(x => x == typeof(CustomClass)));
            Assert.IsTrue(Requester.RegisteredResponseMessageTypes.Any(x => x == typeof(string)));

            Requester.UnregisterResponseMessageReceiver<int>();

            Assert.AreEqual(2, Requester.RegisteredResponseMessageTypes.Count());
            Assert.IsTrue(Requester.RegisteredResponseMessageTypes.Any(x => x == typeof(CustomClass)));
            Assert.IsTrue(Requester.RegisteredResponseMessageTypes.Any(x => x == typeof(string)));
        }

        protected IMessagingSystemFactory MessagingSystemFactory { get; set; }
        protected IDuplexOutputChannel DuplexOutputChannel { get; set; }
        protected IDuplexInputChannel DuplexInputChannel { get; set; }

        protected IMultiTypedMessageSender Requester { get; set; }
        protected IMultiTypedMessageReceiver Responser { get; set; }
    }
}
