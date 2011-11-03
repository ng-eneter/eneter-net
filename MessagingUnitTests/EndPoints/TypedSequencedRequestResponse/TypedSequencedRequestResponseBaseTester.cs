using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eneter.Messaging.EndPoints.TypedSequencedMessages;
using NUnit.Framework;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using System.Threading;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;

namespace Eneter.MessagingUnitTests.EndPoints.TypedSequencedRequestResponse
{
    public abstract class TypedSequencedRequestResponseBaseTester
    {
        protected void Setup(IMessagingSystemFactory messagingSystemFactory, string channelId, ISerializer serializer)
        {
            MessagingSystemFactory = messagingSystemFactory;

            DuplexOutputChannel = MessagingSystemFactory.CreateDuplexOutputChannel(channelId);
            DuplexInputChannel = MessagingSystemFactory.CreateDuplexInputChannel(channelId);

            IDuplexTypedSequencedMessagesFactory aMessageFactory = new DuplexTypedSequencedMessagesFactory(serializer);
            Requester = aMessageFactory.CreateDuplexTypedSequencedMessageSender<int, int>();
            Responser = aMessageFactory.CreateDuplexTypedSequencedMessageReceiver<int, int>();
        }

        [Test]
        public void SendReceive_1Sequence()
        {
            // The test can be performed from more thread therefore we must synchronize.
            AutoResetEvent aMessageReceivedEvent = new AutoResetEvent(false);

            List<TypedSequencedRequestReceivedEventArgs<int>> aReceivedRequests = new List<TypedSequencedRequestReceivedEventArgs<int>>();
            Responser.MessageReceived += (x, y) =>
                {
                    lock (aReceivedRequests)
                    {
                        aReceivedRequests.Add(y);
                    }

                    // Send the response
                    Responser.SendResponseMessage(y.ResponseReceiverId, y.RequestMessage - 1000, "ResponseSequence1", y.IsSequenceCompleted);
                };
            Responser.AttachDuplexInputChannel(DuplexInputChannel);

            List<TypedSequencedResponseReceivedEventArgs<int>> aReceivedResponses = new List<TypedSequencedResponseReceivedEventArgs<int>>();
            Requester.ResponseReceived += (x, y) =>
                {
                    lock (aReceivedResponses)
                    {
                        aReceivedResponses.Add(y);
                    }

                    // Signal that the response message was received -> the loop is closed.
                    if (y.IsSequenceCompleted)
                    {
                        aMessageReceivedEvent.Set();
                    }
                };
            Requester.AttachDuplexOutputChannel(DuplexOutputChannel);

            try
            {
                Requester.SendMessage(2000, "RequestSequence1", false);
                Requester.SendMessage(2001, "RequestSequence1", false);
                Requester.SendMessage(2002, "RequestSequence1", true);

                // Wait for the signal that the message is received.
                //Assert.IsTrue(aMessageReceivedEvent.WaitOne(2000));
                Assert.IsTrue(aMessageReceivedEvent.WaitOne());
            }
            finally
            {
                Requester.DetachDuplexOutputChannel();
                Responser.DetachDuplexInputChannel();
            }

            // Check received values
            Assert.AreEqual(3, aReceivedRequests.Count);
            aReceivedRequests.ForEach(x => Assert.AreEqual("RequestSequence1", x.SequenceId));
            Assert.AreEqual(2000, aReceivedRequests[0].RequestMessage);
            Assert.AreEqual(2001, aReceivedRequests[1].RequestMessage);
            Assert.AreEqual(2002, aReceivedRequests[2].RequestMessage);
            Assert.IsFalse(aReceivedRequests[0].IsSequenceCompleted);
            Assert.IsFalse(aReceivedRequests[1].IsSequenceCompleted);
            Assert.IsTrue(aReceivedRequests[2].IsSequenceCompleted);

            Assert.AreEqual(3, aReceivedResponses.Count);
            aReceivedResponses.ForEach(x => Assert.AreEqual("ResponseSequence1", x.SequenceId));
            Assert.AreEqual(1000, aReceivedResponses[0].ResponseMessage);
            Assert.AreEqual(1001, aReceivedResponses[1].ResponseMessage);
            Assert.AreEqual(1002, aReceivedResponses[2].ResponseMessage);
            Assert.IsFalse(aReceivedResponses[0].IsSequenceCompleted);
            Assert.IsFalse(aReceivedResponses[1].IsSequenceCompleted);
            Assert.IsTrue(aReceivedResponses[2].IsSequenceCompleted);
        }

        [Test]
        public void SendReceive_MoreSequencesInParallel()
        {
            // The test can be performed from more thread therefore we must synchronize.
            AutoResetEvent aMessageReceivedEvent = new AutoResetEvent(false);

            List<TypedSequencedRequestReceivedEventArgs<int>> aReceivedRequests = new List<TypedSequencedRequestReceivedEventArgs<int>>();
            Responser.MessageReceived += (x, y) =>
                {
                    aReceivedRequests.Add(y);

                    // Send the response
                    string aResponseSequenceId = "R" + y.SequenceId;
                    int aResponseMessage = y.RequestMessage + 2000;
                    Responser.SendResponseMessage(y.ResponseReceiverId, aResponseMessage, aResponseSequenceId, y.IsSequenceCompleted);
                };
            Responser.AttachDuplexInputChannel(DuplexInputChannel);

            int aNumberOfCompleted = 0;
            List<TypedSequencedResponseReceivedEventArgs<int>> aReceivedResponses = new List<TypedSequencedResponseReceivedEventArgs<int>>();
            Requester.ResponseReceived += (x, y) =>
                {
                    aReceivedResponses.Add(y);

                    // Signal that the response message was received -> the loop is closed.
                    if (y.IsSequenceCompleted)
                    {
                        ++aNumberOfCompleted;

                        // We have 10 threads pruducing the sequence.
                        if (aNumberOfCompleted == 10)
                        {
                            aMessageReceivedEvent.Set();
                        }
                    }
                };
            Requester.AttachDuplexOutputChannel(DuplexOutputChannel);

            try
            {
                List<Thread> aThreads = new List<Thread>();
                
                // 10 threads producing in parallel one sequence each. Every sequence has 100 fragments.
                for (int i = 0; i < 10; ++i)
                {
                    Thread aThread = new Thread(x =>
                        {
                            string aSequenceId = (string)x;
                            int k = int.Parse(aSequenceId);

                            for (int ii = 0; ii < 100; ++ii)
                            {
                                Requester.SendMessage(100 * k + ii, aSequenceId, ii == 99);
                                Thread.Sleep(1);
                            }
                        });

                    aThreads.Add(aThread);
                }

                for (int iii = 0; iii < 10; ++iii)
                {
                    aThreads[iii].Start(iii.ToString());
                }

                // Wait for the signal that the message is received.
                aMessageReceivedEvent.WaitOne();
                //Assert.IsTrue(aMessageReceivedEvent.WaitOne(3000), "Wait for the whole test - timeout.");
            }
            finally
            {
                Requester.DetachDuplexOutputChannel();
                Responser.DetachDuplexInputChannel();
            }

            // Check received values
            Assert.AreEqual(10 * 100, aReceivedRequests.Count);
            // Check that 10 sequences with 100 fragments were received as requests.
            for (int i = 0; i < 10; ++i)
            {
                Assert.AreEqual(100, aReceivedRequests.Count(x => x.SequenceId == i.ToString()));
            }
            // Check that each sequence has just one completed flag.
            for (int i = 0; i < 10; ++i)
            {
                Assert.AreEqual(1, aReceivedRequests.Count(x => x.SequenceId == i.ToString() && x.IsSequenceCompleted));
            }
            // Check that values (messages) in each sequence are correct.
            for (int i = 0; i < 10; ++i)
            {
                // Note: The method List.FindAll(Predicate x) returning the list is not avsailable in Silverlight.
                //       Therefore we must use a little workaround.
                IEnumerable<TypedSequencedRequestReceivedEventArgs<int>> aSequencedMsgs = aReceivedRequests.Where(x => x.SequenceId == i.ToString());
                List<TypedSequencedRequestReceivedEventArgs<int>> aSequencedMessages = aSequencedMsgs.ToList();

                Assert.AreEqual(100, aSequencedMessages.Count());
                for (int ii = 0; ii < 100; ++ii)
                {
                    Assert.AreEqual(100 * i + ii, aSequencedMessages[ii].RequestMessage);
                }
            }


            // Check responded values
            Assert.AreEqual(10 * 100, aReceivedResponses.Count);
            // Check that 10 sequences with 100 fragments each were received as responses.
            for (int i = 0; i < 10; ++i)
            {
                Assert.AreEqual(100, aReceivedResponses.Count(x => x.SequenceId == "R" + i.ToString()));
            }
            // Check that each sequence has just one completed flag.
            for (int i = 0; i < 10; ++i)
            {
                Assert.AreEqual(1, aReceivedResponses.Count(x => x.SequenceId == "R" + i.ToString() && x.IsSequenceCompleted));
            }
            // Check that values (messages) in each sequence are correct.
            for (int i = 0; i < 10; ++i)
            {
                // Note: The method List.FindAll(Predicate x) returning the list is not avsailable in Silverlight.
                //       Therefore we must use a little workaround.
                IEnumerable<TypedSequencedResponseReceivedEventArgs<int>> aSequencedMsgs = aReceivedResponses.Where(x => x.SequenceId == "R" + i.ToString());
                List<TypedSequencedResponseReceivedEventArgs<int>> aSequencedMessages = aSequencedMsgs.ToList();

                Assert.AreEqual(100, aSequencedMessages.Count());
                for (int ii = 0; ii < 100; ++ii)
                {
                    Assert.AreEqual(100 * i + ii + 2000, aSequencedMessages[ii].ResponseMessage);
                }
            }
        }


        protected IMessagingSystemFactory MessagingSystemFactory { get; set; }
        protected IDuplexOutputChannel DuplexOutputChannel { get; set; }
        protected IDuplexInputChannel DuplexInputChannel { get; set; }


        protected IDuplexTypedSequencedMessageSender<int, int> Requester { get; set; }
        protected IDuplexTypedSequencedMessageReceiver<int, int> Responser { get; set; }
    }
}
