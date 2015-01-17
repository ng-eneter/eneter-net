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

            IMultiTypedMessagesFactory aMessageFactory = new MultiTypedMessagesFactory(serializer);
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
            }

            // Check received values
            Assert.AreEqual(1000, aReceivedMessage1);
            Assert.AreEqual("hello", aReceivedResponse1);

            Assert.IsNotNull(aReceivedMessage2);
            Assert.AreEqual("House", aReceivedMessage2.Name);
            Assert.AreEqual(1000, aReceivedMessage2.Count);
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
        }

        [Test]
        public void RegisterUnregister()
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
            EventHandler<TypedResponseReceivedEventArgs<string>> aResponseHandler = (x, y) =>
            {
                aReceivedResponse1 = y.ResponseMessage;
            };

            // Register
            Requester.RegisterResponseMessageReceiver<string>(aResponseHandler);

            // Unregister the string.
            Requester.UnregisterResponseMessageReceiver<string>();


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
            }

            // Check received values
            Assert.AreEqual(1000, aReceivedMessage1);
            Assert.AreEqual("", aReceivedResponse1);

            Assert.IsNotNull(aReceivedMessage2);
            Assert.AreEqual("House", aReceivedMessage2.Name);
            Assert.AreEqual(1000, aReceivedMessage2.Count);
        }

        protected IMessagingSystemFactory MessagingSystemFactory { get; set; }
        protected IDuplexOutputChannel DuplexOutputChannel { get; set; }
        protected IDuplexInputChannel DuplexInputChannel { get; set; }

        protected IMultiTypedMessageSender Requester { get; set; }
        protected IMultiTypedMessageReceiver Responser { get; set; }
    }
}
