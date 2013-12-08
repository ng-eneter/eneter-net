using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.DataProcessing.MessageQueueing;
using System.Threading;
using Eneter.Messaging.Diagnostic;
using System.Diagnostics;

namespace Eneter.MessagingUnitTests.DataProcessing.MessageQueueing
{
    [TestFixture]
    public class Test_WorkingThread
    {
        [Test]
        public void EnqueueDequeueStop()
        {
            WorkingThread<object> aWorkingThread = new WorkingThread<object>();

            AutoResetEvent aQueueCompleted = new AutoResetEvent(false);
            List<string> aReceivedMessages = new List<string>();
            Action<object> aProcessingCallback = x =>
                {
                    aReceivedMessages.Add((string)x);

                    if (aReceivedMessages.Count == 3)
                    {
                        aQueueCompleted.Set();
                    }
                };

            aWorkingThread.RegisterMessageHandler(aProcessingCallback);

            aWorkingThread.EnqueueMessage("Message1");
            aWorkingThread.EnqueueMessage("Message2");
            aWorkingThread.EnqueueMessage("Message3");

            aQueueCompleted.WaitOne();

            aWorkingThread.UnregisterMessageHandler();

            Assert.AreEqual("Message1", aReceivedMessages[0]);
            Assert.AreEqual("Message2", aReceivedMessages[1]);
            Assert.AreEqual("Message3", aReceivedMessages[2]);
        }

        [Test]
        public void BlockUnblock()
        {
            WorkingThread<object> aWorkingThread1 = new WorkingThread<object>();
            WorkingThread<object> aWorkingThread2 = new WorkingThread<object>();

            Action<object> aProcessingCallback = x => {};

            aWorkingThread1.RegisterMessageHandler(aProcessingCallback);
            aWorkingThread2.RegisterMessageHandler(aProcessingCallback);

            aWorkingThread1.UnregisterMessageHandler();
            aWorkingThread2.UnregisterMessageHandler();
        }

        [Test]
        public void EnqueueDequeueStop_1000000()
        {
            WorkingThread<object> aWorkingThread = new WorkingThread<object>();

            AutoResetEvent aQueueCompleted = new AutoResetEvent(false);
            List<string> aReceivedMessages = new List<string>();
            Action<object> aProcessingCallback = x =>
            {
                aReceivedMessages.Add((string)x);

                if (aReceivedMessages.Count == 1000000)
                {
                    aQueueCompleted.Set();
                }
            };

            aWorkingThread.RegisterMessageHandler(aProcessingCallback);

            Stopwatch aStopWatch = new Stopwatch();
            aStopWatch.Start();

            for (int i = 0; i < 1000000; ++i)
            {
                aWorkingThread.EnqueueMessage("a");
            }

            aQueueCompleted.WaitOne();

            aStopWatch.Stop();
            Console.WriteLine("Elapsed time: {0}", aStopWatch.Elapsed);

            aWorkingThread.UnregisterMessageHandler();
        }

/*
        [Test]
        public void WorkingThreadInvoke_1000000()
        {
            WorkingThreadInvoker anInvoker = new WorkingThreadInvoker();

            AutoResetEvent aQueueCompleted = new AutoResetEvent(false);
            List<string> aReceivedMessages = new List<string>();
            Action<string> aProcessingCallback = x =>
            {
                aReceivedMessages.Add(x);

                if (aReceivedMessages.Count == 1000000)
                {
                    aQueueCompleted.Set();
                }
            };

            anInvoker.Start();

            //EneterTrace.StartProfiler();

            Stopwatch aStopWatch = new Stopwatch();
            aStopWatch.Start();

            for (int i = 0; i < 1000000; ++i)
            {
                anInvoker.Invoke(() => aProcessingCallback("a"));
            }

            aQueueCompleted.WaitOne();

            //EneterTrace.StopProfiler();

            aStopWatch.Stop();
            Console.WriteLine("Elapsed time: {0}", aStopWatch.Elapsed);

            anInvoker.Stop();
        }
*/
    }
}
