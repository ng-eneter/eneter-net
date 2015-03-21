using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.EndPoints.TypedMessages;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.MessagingUnitTests.EndPoints.MultiTypedMessages
{
    public abstract class SyncMultiTypedMessageBaseTester
    {
        [Test]
        public void SyncRequestResponse_MultipleTypes()
        {
            IMultiTypedMessageReceiver aReceiver = MultiTypedMessagesFactory.CreateMultiTypedMessageReceiver();
            aReceiver.RegisterRequestMessageReceiver<int>((x, y) =>
                {
                    aReceiver.SendResponseMessage<string>(y.ResponseReceiverId, y.RequestMessage.ToString());
                });
            aReceiver.RegisterRequestMessageReceiver<string>((x, y) =>
                {
                    aReceiver.SendResponseMessage<int>(y.ResponseReceiverId, y.RequestMessage.Length);
                });

            ISyncMultitypedMessageSender aSender = MultiTypedMessagesFactory.CreateSyncMultiTypedMessageSender();

            try
            {
                aReceiver.AttachDuplexInputChannel(InputChannel);
                aSender.AttachDuplexOutputChannel(OutputChannel);

                string aResult1 = aSender.SendRequestMessage<string, int>(100);
                Assert.AreEqual("100", aResult1);

                int aResult2 = aSender.SendRequestMessage<int, string>("Hello");
                Assert.AreEqual(5, aResult2);
            }
            finally
            {
                aSender.DetachDuplexOutputChannel();
                aReceiver.DetachDuplexInputChannel();
            }
        }

        protected IDuplexInputChannel InputChannel { get; set; }
        protected IDuplexOutputChannel OutputChannel { get; set; }
        protected IMultiTypedMessagesFactory MultiTypedMessagesFactory { get; set; }
    }
}
