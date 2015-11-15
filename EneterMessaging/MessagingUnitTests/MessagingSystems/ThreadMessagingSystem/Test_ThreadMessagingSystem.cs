using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.ThreadMessagingSystem;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using System.Threading;
using Eneter.Messaging.Diagnostic;
using System.IO;

namespace Eneter.MessagingUnitTests.MessagingSystems.ThreadMessagingSystem
{
    [TestFixture]
    public class Test_ThreadMessagingSystem : BaseTester
    {
        [SetUp]
        public void Setup()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            //EneterTrace.TraceLog = new StreamWriter("d:/tracefile.txt");

            MessagingSystemFactory = new ThreadMessagingSystemFactory();
        }

        [TearDown]
        public void TearDown()
        {
            EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Short;
            if (EneterTrace.TraceLog != null)
            {
                EneterTrace.TraceLog.Dispose();
                EneterTrace.TraceLog = null;
            }
        }

        /*
        [Test]
        public void SendMessage()
        {
            IOutputChannel anOutputChannel = myThreadMessagingSystemFactory.CreateOutputChannel("Channel1");
            IInputChannel anInputChannel = myThreadMessagingSystemFactory.CreateInputChannel("Channel1");

            // Helping thread signaling end of message handling
            object aLocker = new object();
            Thread aHelperThread = new Thread( () => 
                {
                    lock (aLocker)
                    {
                        Monitor.Wait(aLocker);
                    }
                });
            aHelperThread.Start();

            // Observe the input channel
            List<string> aReceivedMessages = new List<string>();
            anInputChannel.MessageReceived += (x, y) =>
                {
                    aReceivedMessages.Add(y.Message);

                    if (aReceivedMessages.Count == 100)
                    {
                        lock (aLocker)
                        {
                            Monitor.Pulse(aLocker);
                        }
                    }
                };
            anInputChannel.StartListening();

            // Send 100 messages
            for (int i = 0; i < 100; ++i)
            {
                anOutputChannel.SendMessage(i.ToString());
            }

            bool isHelperThreadTerminated = aHelperThread.Join(100);
            if (!isHelperThreadTerminated)
            {
                aHelperThread.Abort();
            }

            anInputChannel.StopListening();

            // Check
            Assert.IsTrue(isHelperThreadTerminated);
            Assert.AreEqual(100, aReceivedMessages.Count);
        }

        [Test]
        public void StopListening()
        {
            IOutputChannel anOutputChannel = myThreadMessagingSystemFactory.CreateOutputChannel("Channel1");
            IInputChannel anInputChannel = myThreadMessagingSystemFactory.CreateInputChannel("Channel1");

            // Helping thread signaling end of message handling
            object aLocker = new object();
            Thread aHelperThread = new Thread(() =>
            {
                lock (aLocker)
                {
                    Monitor.Wait(aLocker);
                }
            });
            aHelperThread.Start();


            Thread aListenerWorkingThread = null;
            anInputChannel.MessageReceived += (x, y) =>
            {
                aListenerWorkingThread = Thread.CurrentThread;
                lock (aLocker)
                {
                    // Signal the waiting thread
                    Monitor.Pulse(aLocker);
                }
            };
            anInputChannel.StartListening();

            anOutputChannel.SendMessage("A dummy message");

            // Wait more that internal waitings.
            bool isHelperThreadTerminated = aHelperThread.Join(100);
            if (!isHelperThreadTerminated)
            {
                aHelperThread.Abort();
            }

            // Stop the listening
            anInputChannel.StopListening();

            Assert.IsTrue(isHelperThreadTerminated);
            Assert.AreEqual(ThreadState.Stopped, aListenerWorkingThread.ThreadState);
        }

        [Test]
        public void MultithreadSendMessage()
        {
            IOutputChannel anOutputChannel = myThreadMessagingSystemFactory.CreateOutputChannel("Channel1");
            IInputChannel anInputChannel = myThreadMessagingSystemFactory.CreateInputChannel("Channel1");

            // Helping thread signaling end of message handling
            object aLocker = new object();
            Thread aHelperThread = new Thread(() =>
                {
                    lock (aLocker)
                    {
                        Monitor.Wait(aLocker);
                    }
                });
            aHelperThread.Start();

            // Observe the input channel
            List<string> aReceivedMessages = new List<string>();
            anInputChannel.MessageReceived += (x, y) =>
            {
                aReceivedMessages.Add(y.Message);

                Console.WriteLine(aReceivedMessages.Count.ToString() + " " + y.Message);

                if (aReceivedMessages.Count == 500)
                {
                    lock (aLocker)
                    {
                        Monitor.Pulse(aLocker);
                    }
                }
            };
            anInputChannel.StartListening();

            // Create 50 competing threads
            List<Thread> aThreads = new List<Thread>();
            for (int t = 0; t < 50; ++t)
            {
                Thread aThread = new Thread(() =>
                    {
                        // Send 100 messages
                        for (int i = 0; i < 10; ++i)
                        {
                            Thread.Sleep(1); // To mix the order of threads. (othewise it would go thread by thread)
                            anOutputChannel.SendMessage(Thread.CurrentThread.ManagedThreadId.ToString());
                        }
                    });
                aThreads.Add(aThread);
            }

            // Start sending from threads
            aThreads.ForEach(x => x.Start());

            // Wait for all threads.
            aThreads.ForEach(x => Assert.IsTrue(x.Join(3000)));

            Assert.IsTrue(aHelperThread.Join(5000));

            anInputChannel.StopListening();

            // Check
            Assert.AreEqual(500, aReceivedMessages.Count);
        }

        private ThreadMessagingSystemFactory myThreadMessagingSystemFactory;*/
    }
}
