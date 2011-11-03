
#if !SILVERLIGHT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
using System.Net.Sockets;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using System.Threading;
using Eneter.Messaging.Diagnostic;
using System.IO;

namespace Eneter.MessagingUnitTests.MessagingSystems.TcpMessagingSystem
{
    [TestFixture]
    public class Test_TcpMessagingSystem_Synchronous : MessagingSystemBaseTester
    {
        [SetUp]
        public void Setup()
        {
            //EneterTrace.DetailLevel = EneterTrace.EDetailLevel.Debug;
            //EneterTrace.TraceLog = new StreamWriter("d:/tracefile.txt");

            MessagingSystemFactory = new TcpMessagingSystemFactory();
            ChannelId = "tcp://127.0.0.1:8091/";
        }

        [Test]
        [ExpectedException(typeof(SocketException))]
        public override void A07_StopListening()
        {
            try
            {
                base.A07_StopListening();
            }
            catch (SocketException err)
            {
                // Error is that the connection cannot be established.
                // Note: it is good because it means the listener was correctly stoped :)
                if (err.ErrorCode == 10061)
                {
                    throw;
                }

                throw new Exception("Incorrect socket exception was detected.", err);
            }
        }

        [Test]
        public void SendReceiveMessage_5threads_10000messages()
        {
            IInputChannel anInputChannel = MessagingSystemFactory.CreateInputChannel(ChannelId);
            IOutputChannel anOutputChannel = MessagingSystemFactory.CreateOutputChannel(ChannelId);

            // Helping thread signaling end of message handling
            ManualResetEvent anEverythingProcessedEvent = new ManualResetEvent(false);

            // Observe the input channel
            List<string> aReceivedMessages = new List<string>();
            anInputChannel.MessageReceived += (x, y) =>
                {
                    lock (aReceivedMessages)
                    {
                        aReceivedMessages.Add((string)y.Message);

                        if (aReceivedMessages.Count == 2000 * 5)
                        {
                            anEverythingProcessedEvent.Set();
                        }
                    }
                };

            try
            {
                // 4 competing threads
                List<Thread> aThreads = new List<Thread>();
                for (int i = 0; i < 5; ++i)
                {
                    Thread aThread = new Thread(() =>
                        {
                            // Send messages
                            for (int ii = 0; ii < 2000; ++ii)
                            {
                                anOutputChannel.SendMessage(ii.ToString());
                            }
                        });
                    aThreads.Add(aThread);
                }

                anInputChannel.StartListening();

                aThreads.ForEach(x => x.Start());
                aThreads.ForEach(x => x.Join());

                Assert.IsTrue(anEverythingProcessedEvent.WaitOne(5000), "Timeout for processing of messages.");
            }
            finally
            {
                anInputChannel.StopListening();
            }

            // Check
            Assert.AreEqual(2000 * 5, aReceivedMessages.Count);
        }
    }
}


#endif