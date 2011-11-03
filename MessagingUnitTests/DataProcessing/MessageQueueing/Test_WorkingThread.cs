using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.DataProcessing.MessageQueueing;

namespace Eneter.MessagingUnitTests.DataProcessing.MessageQueueing
{
    [TestFixture]
    public class Test_WorkingThread
    {
        [Test]
        public void EnqueueDequeueStop()
        {
            WorkingThread<object> aWorkingThread = new WorkingThread<object>();

            List<string> aReceivedMessages = new List<string>();
            Action<object> aProcessingCallback = x =>
                {
                    aReceivedMessages.Add((string)x);
                };

            aWorkingThread.RegisterMessageHandler(aProcessingCallback);

            aWorkingThread.EnqueueMessage("Message1");
            aWorkingThread.EnqueueMessage("Message2");
            aWorkingThread.EnqueueMessage("Message3");

            aWorkingThread.UnregisterMessageHandler();
        }

        [Test]
        public void BlockUnblock()
        {
            WorkingThread<object> aWorkingThread1 = new WorkingThread<object>("aaa1");
            WorkingThread<object> aWorkingThread2 = new WorkingThread<object>("aaa1");

            Action<object> aProcessingCallback = x => {};

            aWorkingThread1.RegisterMessageHandler(aProcessingCallback);
            aWorkingThread2.RegisterMessageHandler(aProcessingCallback);

            aWorkingThread1.UnregisterMessageHandler();
            aWorkingThread2.UnregisterMessageHandler();
        }
    }
}
