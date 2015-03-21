using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.EndPoints.TypedMessages;
using NUnit.Framework;
using System.Threading;
using Eneter.Messaging.Threading.Dispatching;
using System.Windows.Threading;

namespace Eneter.MessagingUnitTests.EndPoints.SyncTypedMessages
{
    public abstract class SyncTypedMessagesBaseTester
    {
        [Test]
        public void SyncRequestResponse()
        {
            IDuplexTypedMessageReceiver<string, int> aReceiver = DuplexTypedMessagesFactory.CreateDuplexTypedMessageReceiver<string, int>();
            aReceiver.MessageReceived += (x, y) =>
                {
                    aReceiver.SendResponseMessage(y.ResponseReceiverId, (y.RequestMessage * 10).ToString());
                };

            ISyncDuplexTypedMessageSender<string, int> aSender = DuplexTypedMessagesFactory.CreateSyncDuplexTypedMessageSender<string, int>();

            try
            {
                aReceiver.AttachDuplexInputChannel(InputChannel);
                aSender.AttachDuplexOutputChannel(OutputChannel);

                string aResult = aSender.SendRequestMessage(100);

                Assert.AreEqual("1000", aResult);

            }
            finally
            {
                aSender.DetachDuplexOutputChannel();
                aReceiver.DetachDuplexInputChannel();
            }
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void ConnectionClosedDuringWaitingForResponse()
        {
            IDuplexTypedMessageReceiver<int, int> aReceiver = DuplexTypedMessagesFactory.CreateDuplexTypedMessageReceiver<int, int>();
            aReceiver.MessageReceived += (x, y) =>
            {
                // Disconnect the cient.
                aReceiver.AttachedDuplexInputChannel.DisconnectResponseReceiver(y.ResponseReceiverId);
            };

            ISyncDuplexTypedMessageSender<int, int> aSender = DuplexTypedMessagesFactory.CreateSyncDuplexTypedMessageSender<int, int>();

            try
            {
                aReceiver.AttachDuplexInputChannel(InputChannel);
                aSender.AttachDuplexOutputChannel(OutputChannel);

                int aResult = aSender.SendRequestMessage(100);

                // The previous call should raise the exception there we failed if we are here.
                Assert.Fail();
            }
            finally
            {
                aSender.DetachDuplexOutputChannel();
                aReceiver.DetachDuplexInputChannel();
            }
        }

        [Test]
        public void DetachOutputChannelDuringWaitingForResponse()
        {
            IDuplexTypedMessageReceiver<int, int> aReceiver = DuplexTypedMessagesFactory.CreateDuplexTypedMessageReceiver<int, int>();

            ISyncDuplexTypedMessageSender<int, int> aSender = DuplexTypedMessagesFactory.CreateSyncDuplexTypedMessageSender<int, int>();

            try
            {
                aReceiver.AttachDuplexInputChannel(InputChannel);
                aSender.AttachDuplexOutputChannel(OutputChannel);

                // Send the request from a different thread.
                AutoResetEvent aWaitingInterrupted = new AutoResetEvent(false);
                Exception aCaughtException = null;
                ThreadPool.QueueUserWorkItem(x =>
                    {
                        try
                        {
                            aSender.SendRequestMessage(100);
                        }
                        catch (Exception err)
                        {
                            aCaughtException = err;
                        }

                        aWaitingInterrupted.Set();
                    });

                Thread.Sleep(100);

                // Detach the output channel while other thread waits for the response.
                aSender.DetachDuplexOutputChannel();

                Assert.IsTrue(aWaitingInterrupted.WaitOne(50000));

                Assert.IsTrue(aCaughtException != null && aCaughtException is InvalidOperationException);
            }
            finally
            {
                aSender.DetachDuplexOutputChannel();
                aReceiver.DetachDuplexInputChannel();
            }
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException))]
        public void WaitingForResponseTimeouted()
        {
            IDuplexTypedMessageReceiver<int, int> aReceiver = DuplexTypedMessagesFactory.CreateDuplexTypedMessageReceiver<int, int>();

            // Create sender expecting the response within 500 ms.
            IDuplexTypedMessagesFactory aSenderFactory = new DuplexTypedMessagesFactory(TimeSpan.FromMilliseconds(500));
            ISyncDuplexTypedMessageSender<int, int> aSender = aSenderFactory.CreateSyncDuplexTypedMessageSender<int, int>();

            try
            {
                aReceiver.AttachDuplexInputChannel(InputChannel);
                aSender.AttachDuplexOutputChannel(OutputChannel);

                aSender.SendRequestMessage(100);
            }
            finally
            {
                aSender.DetachDuplexOutputChannel();
                aReceiver.DetachDuplexInputChannel();
            }
        }

        [Test]
        public void ThreadDispatching()
        {
            IDuplexTypedMessageReceiver<string, int> aReceiver = DuplexTypedMessagesFactory.CreateDuplexTypedMessageReceiver<string, int>();
            aReceiver.MessageReceived += (x, y) =>
            {
                aReceiver.SendResponseMessage(y.ResponseReceiverId, (y.RequestMessage * 10).ToString());
            };

            // Set windows working thread dispatcher.
            Dispatcher aWindowsDispatcher = WindowsDispatching.StartNewWindowsDispatcher();
            WindowsDispatching aThreadDispatching = new WindowsDispatching(aWindowsDispatcher);
            ((DuplexTypedMessagesFactory)DuplexTypedMessagesFactory).SyncDuplexTypedSenderThreadMode = aThreadDispatching;

            ISyncDuplexTypedMessageSender<string, int> aSender = DuplexTypedMessagesFactory.CreateSyncDuplexTypedMessageSender<string, int>();

            int aOpenConnectionThreadId = 0;
            aSender.ConnectionOpened += (x, y) =>
                {
                    aOpenConnectionThreadId = Thread.CurrentThread.ManagedThreadId;
                };

            ManualResetEvent aConnectionClosedEvent = new ManualResetEvent(false);
            int aCloseConnectionThreadId = 0;
            aSender.ConnectionClosed += (x, y) =>
                {
                    aCloseConnectionThreadId = Thread.CurrentThread.ManagedThreadId;
                    aConnectionClosedEvent.Set();
                };

            try
            {
                aReceiver.AttachDuplexInputChannel(InputChannel);
                aSender.AttachDuplexOutputChannel(OutputChannel);

                string aResult = aSender.SendRequestMessage(100);

                aReceiver.AttachedDuplexInputChannel.DisconnectResponseReceiver(aSender.AttachedDuplexOutputChannel.ResponseReceiverId);
                aConnectionClosedEvent.WaitOne();

                Assert.AreEqual("1000", aResult);
                Assert.AreEqual(aWindowsDispatcher.Thread.ManagedThreadId, aOpenConnectionThreadId);
                Assert.AreEqual(aWindowsDispatcher.Thread.ManagedThreadId, aCloseConnectionThreadId);
            }
            finally
            {
                aSender.DetachDuplexOutputChannel();
                aReceiver.DetachDuplexInputChannel();

                if (aWindowsDispatcher != null)
                {
                    WindowsDispatching.StopWindowsDispatcher(aWindowsDispatcher);
                }
            }
        }

        protected IDuplexInputChannel InputChannel { get; set; }
        protected IDuplexOutputChannel OutputChannel { get; set; }
        protected IDuplexTypedMessagesFactory DuplexTypedMessagesFactory { get; set; }
    }
}
