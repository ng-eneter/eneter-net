using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Eneter.Messaging.EndPoints.Commands;
using Eneter.Messaging.EndPoints.TypedSequencedMessages;
using Eneter.Messaging.EndPoints.StringMessages;
using Eneter.Messaging.EndPoints.TypedMessages;
using Eneter.Messaging.Nodes.ChannelWrapper;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.SynchronousMessagingSystem;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.DataProcessing.Sequencing;
using System.Threading;
using Eneter.Messaging.MessagingSystems.Composites.ReliableMessagingComposit;

namespace Eneter.MessagingUnitTests.EndPoints.ReliableCommands
{
    public abstract class ReliableCommandTesterBase
    {
        [Test]
        public void ConnectDisconnectCommand()
        {
            IMessagingSystemFactory anUnderlyingMessaging = new SynchronousMessagingSystemFactory();
            IReliableMessagingFactory aMessagingSystemFactory = new ReliableMessagingFactory(anUnderlyingMessaging, Serializer, TimeSpan.FromMilliseconds(12000));

            IReliableDuplexInputChannel aDuplexInputChannel = aMessagingSystemFactory.CreateDuplexInputChannel("Channel1");
            IReliableDuplexOutputChannel aDuplexOutputChannel = aMessagingSystemFactory.CreateDuplexOutputChannel("Channel1");

            IReliableCommandsFactory aCommandsFactory = new ReliableCommandsFactory(Serializer);

            IReliableCommand<string, int> aCommand = aCommandsFactory.CreateReliableCommand<string, int>(x => { }, EProcessingStrategy.MultiThread);

            string aConnectedProxyId = "";
            aCommand.CommandProxyConnected += (x, y) =>
            {
                aConnectedProxyId = y.CommandProxyId;
            };

            string aDisconnectedProxyId = "";
            aCommand.CommandProxyDisconnected += (x, y) =>
            {
                aDisconnectedProxyId = y.CommandProxyId;
            };

            IReliableCommandProxy<string, int> aCommandProxy = aCommandsFactory.CreateReliableCommandProxy<string, int>();

            aCommand.AttachReliableInputChannel(aDuplexInputChannel);
            aCommandProxy.AttachReliableOutputChannel(aDuplexOutputChannel);

            Assert.AreNotEqual("", aConnectedProxyId);

            aCommandProxy.DetachReliableOutputChannel();

            Assert.AreNotEqual("", aDisconnectedProxyId);
        }

        [Test]
        public void ExecuteCommand()
        {
            IMessagingSystemFactory anUnderlyingMessagingFactory = new SynchronousMessagingSystemFactory();
            IReliableMessagingFactory aMessagingSystemFactory = new ReliableMessagingFactory(anUnderlyingMessagingFactory, Serializer, TimeSpan.FromMilliseconds(12000));

            IReliableDuplexInputChannel aDuplexInputChannel = aMessagingSystemFactory.CreateDuplexInputChannel("Channel1");
            IReliableDuplexOutputChannel aDuplexOutputChannel = aMessagingSystemFactory.CreateDuplexOutputChannel("Channel1");

            IReliableCommandsFactory aCommandsFactory = new ReliableCommandsFactory(Serializer);

            string aResponseMessageId = "123";
            IReliableCommand<string, int> aCommand = aCommandsFactory.CreateReliableCommand<string, int>(x =>
                {
                    DataFragment<int> anInputData = x.DequeueInputData();
                    string aResponse = anInputData.Data.ToString();

                    aResponseMessageId = x.Response(ECommandState.Completed, aResponse);
                }, EProcessingStrategy.MultiThread);

            string aDeliveredResponseMessageId = "";
            aCommand.ResponseMessageDelivered += (x, y) =>
                {
                    aDeliveredResponseMessageId = y.MessageId;
                };


            string aConnectedProxyId = "";
            aCommand.CommandProxyConnected += (x, y) =>
                {
                    aConnectedProxyId = y.CommandProxyId;
                };

            string aDisconnectedProxyId = "";
            aCommand.CommandProxyDisconnected += (x, y) =>
                {
                    aDisconnectedProxyId = y.CommandProxyId;
                };

            IReliableCommandProxy<string, int> aCommandProxy = aCommandsFactory.CreateReliableCommandProxy<string, int>();

            ManualResetEvent aResponseReceivedEvent = new ManualResetEvent(false);

            CommandResponseReceivedEventArgs<string> aReceivedResponse = null;
            aCommandProxy.CommandResponseReceived += (x, y) =>
                {
                    aReceivedResponse = y;

                    aResponseReceivedEvent.Set();
                };

            string aDeliveredMessageId = "";
            aCommandProxy.MessageDelivered += (x, y) =>
                {
                    aDeliveredMessageId = y.MessageId;
                };

            aCommand.AttachReliableInputChannel(aDuplexInputChannel);
            aCommandProxy.AttachReliableOutputChannel(aDuplexOutputChannel);

            Assert.AreNotEqual("", aConnectedProxyId);

            string aMessageId = aCommandProxy.Execute("MyTag", 100);

            aResponseReceivedEvent.WaitOne();

            Assert.IsNotNull(aReceivedResponse);
            Assert.AreEqual("100", aReceivedResponse.ReturnData);
            Assert.AreEqual("NA", aReceivedResponse.SequenceId);
            Assert.IsTrue(aReceivedResponse.IsSequenceCompleted);
            Assert.AreEqual("", aReceivedResponse.CommandError);
            Assert.IsNull(aReceivedResponse.ReceivingError);
            Assert.AreEqual(ECommandState.Completed, aReceivedResponse.CommandState);
            Assert.AreEqual("MyTag", aReceivedResponse.CommandId);

            Assert.AreEqual(aMessageId, aDeliveredMessageId);
            Assert.AreEqual(aResponseMessageId, aDeliveredResponseMessageId);
        }

        [Test]
        public void ExecuteDisconnectCommand()
        {
            IMessagingSystemFactory anUnderlyingMessaging = new SynchronousMessagingSystemFactory();
            IReliableMessagingFactory aMessagingSystemFactory = new ReliableMessagingFactory(anUnderlyingMessaging, Serializer, TimeSpan.FromMilliseconds(12000));

            IReliableDuplexInputChannel aDuplexInputChannel = aMessagingSystemFactory.CreateDuplexInputChannel("Channel1");
            IReliableDuplexOutputChannel aDuplexOutputChannel = aMessagingSystemFactory.CreateDuplexOutputChannel("Channel1");

            IReliableCommandsFactory aCommandsFactory = new ReliableCommandsFactory(Serializer);

            ManualResetEvent aCommandProcessedEvent = new ManualResetEvent(false);

            bool wasDisconnected = false;
            IReliableCommand<string, int> aCommand = aCommandsFactory.CreateReliableCommand<string, int>(x =>
            {
                try
                {
                    // Get input data
                    DataFragment<int> anInputData = x.DequeueInputData();

                    // Simulate progress of executed command
                    for (int i = 0; i < 100; ++i)
                    {
                        Thread.Sleep(10);

                        // If the command proxy was disconnected.
                        if (!x.IsCommandProxyConnected)
                        {
                            wasDisconnected = true;
                            return;
                        }

                    }

                    // NOte: the command is supposed to be disconnected, therefore
                    //       this response should not occur. 
                    string aResponse = anInputData.Data.ToString();
                    x.Response(ECommandState.Completed, aResponse);
                }
                finally
                {
                    aCommandProcessedEvent.Set();
                }

            }, EProcessingStrategy.MultiThread);

            IReliableCommandProxy<string, int> aCommandProxy = aCommandsFactory.CreateReliableCommandProxy<string, int>();

            aCommand.AttachReliableInputChannel(aDuplexInputChannel);
            aCommandProxy.AttachReliableOutputChannel(aDuplexOutputChannel);

            aCommandProxy.Execute("MyTag", 100);

            aCommandProxy.DetachReliableOutputChannel();

            aCommandProcessedEvent.WaitOne();

            Assert.IsTrue(wasDisconnected);
        }

        [Test]
        public void ExecuteMultipleCommandsMultiThread()
        {
            IMessagingSystemFactory anUnderlyingMessaging = new SynchronousMessagingSystemFactory();
            IReliableMessagingFactory aMessagingSystemFactory = new ReliableMessagingFactory(anUnderlyingMessaging, Serializer, TimeSpan.FromMilliseconds(12000));

            IReliableDuplexInputChannel aDuplexInputChannel = aMessagingSystemFactory.CreateDuplexInputChannel("Channel1");
            IReliableDuplexOutputChannel aDuplexOutputChannel = aMessagingSystemFactory.CreateDuplexOutputChannel("Channel1");

            IReliableCommandsFactory aCommandsFactory = new ReliableCommandsFactory(Serializer);

            List<string> aResponseMsgIds = new List<string>();
            IReliableCommand<string, int> aCommand = aCommandsFactory.CreateReliableCommand<string, int>(x =>
                {
                    DataFragment<int> anInputData = x.DequeueInputData();

                    for (int i = 0; i < 100; ++i)
                    {
                        Thread.Sleep(10);
                    }

                    lock (aResponseMsgIds)
                    {
                        string aResponse = anInputData.Data.ToString();
                        aResponseMsgIds.Add(x.Response(ECommandState.Completed, aResponse));
                    }
                }, EProcessingStrategy.MultiThread);
            

            List<string> aDeliveredResponseMsgIds = new List<string>();
            aCommand.ResponseMessageDelivered += (x, y) =>
                {
                    lock (aDeliveredResponseMsgIds)
                    {
                        aDeliveredResponseMsgIds.Add(y.MessageId);
                    }
                };


            IReliableCommandProxy<string, int> aCommandProxy = aCommandsFactory.CreateReliableCommandProxy<string, int>();

            ManualResetEvent aResponseReceivedEvent = new ManualResetEvent(false);

            List<CommandResponseReceivedEventArgs<string>> aReceivedResponses = new List<CommandResponseReceivedEventArgs<string>>();
            aCommandProxy.CommandResponseReceived += (x, y) =>
                {
                    lock (aReceivedResponses)
                    {
                        aReceivedResponses.Add(y);

                        if (aReceivedResponses.Count == 3)
                        {
                            aResponseReceivedEvent.Set();
                        }
                    }
                };

            List<string> aDeliveredMsgIds = new List<string>();
            aCommandProxy.MessageDelivered += (x, y) =>
                {
                    lock (aDeliveredMsgIds)
                    {
                        aDeliveredMsgIds.Add(y.MessageId);
                    }
                };

            aCommand.AttachReliableInputChannel(aDuplexInputChannel);
            aCommandProxy.AttachReliableOutputChannel(aDuplexOutputChannel);

            List<string> aMsgIds = new List<string>();
            aMsgIds.Add(aCommandProxy.Execute("CommandId1", 100));
            aMsgIds.Add(aCommandProxy.Execute("CommandId2", 200));
            aMsgIds.Add(aCommandProxy.Execute("CommandId3", 300));

            aResponseReceivedEvent.WaitOne();

            Assert.AreEqual(3, aReceivedResponses.Count);
            
            Assert.IsTrue(aReceivedResponses.Any(x => x.ReturnData == "100" && x.CommandId == "CommandId1"));
            Assert.IsTrue(aReceivedResponses.Any(x => x.ReturnData == "200" && x.CommandId == "CommandId2"));
            Assert.IsTrue(aReceivedResponses.Any(x => x.ReturnData == "300" && x.CommandId == "CommandId3"));

            Assert.AreEqual("NA", aReceivedResponses[0].SequenceId);
            Assert.IsTrue(aReceivedResponses[0].IsSequenceCompleted);
            Assert.AreEqual("", aReceivedResponses[0].CommandError);
            Assert.IsNull(aReceivedResponses[0].ReceivingError);
            Assert.AreEqual(ECommandState.Completed, aReceivedResponses[0].CommandState);

            Assert.AreEqual("NA", aReceivedResponses[1].SequenceId);
            Assert.IsTrue(aReceivedResponses[1].IsSequenceCompleted);
            Assert.AreEqual("", aReceivedResponses[1].CommandError);
            Assert.IsNull(aReceivedResponses[1].ReceivingError);
            Assert.AreEqual(ECommandState.Completed, aReceivedResponses[1].CommandState);

            Assert.AreEqual("NA", aReceivedResponses[2].SequenceId);
            Assert.IsTrue(aReceivedResponses[2].IsSequenceCompleted);
            Assert.AreEqual("", aReceivedResponses[2].CommandError);
            Assert.IsNull(aReceivedResponses[0].ReceivingError);
            Assert.AreEqual(ECommandState.Completed, aReceivedResponses[2].CommandState);

            aMsgIds.Sort();
            aDeliveredMsgIds.Sort();
            Assert.IsTrue(aMsgIds.SequenceEqual(aDeliveredMsgIds));

            aResponseMsgIds.Sort();
            aDeliveredResponseMsgIds.Sort();
            Assert.IsTrue(aResponseMsgIds.SequenceEqual(aDeliveredResponseMsgIds));
        }

        [Test]
        public void PauseResumeCommand()
        {
            IMessagingSystemFactory anUnderlyingMessaging = new SynchronousMessagingSystemFactory();
            IReliableMessagingFactory aMessagingSystemFactory = new ReliableMessagingFactory(anUnderlyingMessaging, Serializer, TimeSpan.FromMilliseconds(12000));

            IReliableDuplexInputChannel aDuplexInputChannel = aMessagingSystemFactory.CreateDuplexInputChannel("Channel1");
            IReliableDuplexOutputChannel aDuplexOutputChannel = aMessagingSystemFactory.CreateDuplexOutputChannel("Channel1");

            IReliableCommandsFactory aCommandsFactory = new ReliableCommandsFactory(Serializer);

            bool wasPaused = false;
            IReliableCommand<string, int> aCommand = aCommandsFactory.CreateReliableCommand<string, int>(x =>
                {
                    // Get input data
                    DataFragment<int> anInputData = x.DequeueInputData();

                    for (int i = 0; i < 100; ++i)
                    {
                        Thread.Sleep(10);

                        if (x.CurrentRequest == ECommandRequest.Pause)
                        {
                            wasPaused = true;
                            x.WaitIfPause();
                        }
                    }

                    string aResponse = anInputData.Data.ToString();
                    x.Response(ECommandState.Completed, aResponse);

                }, EProcessingStrategy.MultiThread);

            IReliableCommandProxy<string, int> aCommandProxy = aCommandsFactory.CreateReliableCommandProxy<string, int>();

            List<string> aDeliveredMsgIds = new List<string>();
            aCommandProxy.MessageDelivered += (x, y) =>
                {
                    lock (aDeliveredMsgIds)
                    {
                        aDeliveredMsgIds.Add(y.MessageId);
                    }
                };

            ManualResetEvent aResponseReceivedEvent = new ManualResetEvent(false);

            CommandResponseReceivedEventArgs<string> aReceivedResponse = null;
            aCommandProxy.CommandResponseReceived += (x, y) =>
            {
                aReceivedResponse = y;
                aResponseReceivedEvent.Set();
            };


            aCommand.AttachReliableInputChannel(aDuplexInputChannel);
            aCommandProxy.AttachReliableOutputChannel(aDuplexOutputChannel);

            List<string> aMsgIds = new List<string>();

            aMsgIds.Add(aCommandProxy.Execute("MyTag", 100));
            
            aMsgIds.Add(aCommandProxy.Pause("MyTag"));
            Thread.Sleep(500);
            aMsgIds.Add(aCommandProxy.Resume("MyTag"));

            aResponseReceivedEvent.WaitOne();

            Assert.IsTrue(wasPaused);
            Assert.IsNotNull(aReceivedResponse);
            Assert.AreEqual("100", aReceivedResponse.ReturnData);
            Assert.AreEqual("NA", aReceivedResponse.SequenceId);
            Assert.IsTrue(aReceivedResponse.IsSequenceCompleted);
            Assert.AreEqual("", aReceivedResponse.CommandError);
            Assert.IsNull(aReceivedResponse.ReceivingError);
            Assert.AreEqual(ECommandState.Completed, aReceivedResponse.CommandState);
            Assert.AreEqual("MyTag", aReceivedResponse.CommandId);

            aMsgIds.Sort();
            aDeliveredMsgIds.Sort();
            Assert.IsTrue(aMsgIds.SequenceEqual(aDeliveredMsgIds));
        }

        [Test]
        public void PauseCancelCommand()
        {
            IMessagingSystemFactory anUnderlyingMessaging = new SynchronousMessagingSystemFactory();
            IReliableMessagingFactory aMessagingSystemFactory = new ReliableMessagingFactory(anUnderlyingMessaging, Serializer, TimeSpan.FromMilliseconds(12000));

            IReliableDuplexInputChannel aDuplexInputChannel = aMessagingSystemFactory.CreateDuplexInputChannel("Channel1");
            IReliableDuplexOutputChannel aDuplexOutputChannel = aMessagingSystemFactory.CreateDuplexOutputChannel("Channel1");

            IReliableCommandsFactory aCommandsFactory = new ReliableCommandsFactory(Serializer);

            bool wasPaused = false;
            bool wasCanceled = false;
            IReliableCommand<string, int> aCommand = aCommandsFactory.CreateReliableCommand<string, int>(x =>
                {
                    // Get input data
                    DataFragment<int> anInputData = x.DequeueInputData();

                    for (int i = 0; i < 100; ++i)
                    {
                        Thread.Sleep(10);

                        if (x.CurrentRequest == ECommandRequest.Pause)
                        {
                            wasPaused = true;
                            x.WaitIfPause();
                        }

                        if (x.CurrentRequest == ECommandRequest.Cancel)
                        {
                            wasCanceled = true;
                            x.ResponseCancel();
                            return;
                        }
                    }

                    // NOte: the command is supposed to be canceled, therefore
                    //       this response should not occur. 
                    string aResponse = anInputData.Data.ToString();
                    x.Response(ECommandState.Completed, aResponse);

                }, EProcessingStrategy.MultiThread);

            IReliableCommandProxy<string, int> aCommandProxy = aCommandsFactory.CreateReliableCommandProxy<string, int>();

            ManualResetEvent aResponseReceivedEvent = new ManualResetEvent(false);

            CommandResponseReceivedEventArgs<string> aReceivedResponse = null;
            aCommandProxy.CommandResponseReceived += (x, y) =>
            {
                aReceivedResponse = y;
                aResponseReceivedEvent.Set();
            };

            List<string> aDeliveredMsgIds = new List<string>();
            aCommandProxy.MessageDelivered += (x, y) =>
            {
                lock (aDeliveredMsgIds)
                {
                    aDeliveredMsgIds.Add(y.MessageId);
                }
            };


            aCommand.AttachReliableInputChannel(aDuplexInputChannel);
            aCommandProxy.AttachReliableOutputChannel(aDuplexOutputChannel);

            List<string> aMsgIds = new List<string>();

            aMsgIds.Add(aCommandProxy.Execute("MyTag", 100));

            aMsgIds.Add(aCommandProxy.Pause("MyTag"));
            Thread.Sleep(500);
            aMsgIds.Add(aCommandProxy.Cancel("MyTag"));

            aResponseReceivedEvent.WaitOne();

            Assert.IsTrue(wasPaused);
            Assert.IsTrue(wasCanceled);
            Assert.IsNotNull(aReceivedResponse);
            Assert.AreEqual(null, aReceivedResponse.ReturnData);
            Assert.AreEqual("NA", aReceivedResponse.SequenceId);
            Assert.IsTrue(aReceivedResponse.IsSequenceCompleted);
            Assert.AreEqual("", aReceivedResponse.CommandError);
            Assert.IsNull(aReceivedResponse.ReceivingError);
            Assert.AreEqual(ECommandState.Canceled, aReceivedResponse.CommandState);
            Assert.AreEqual("MyTag", aReceivedResponse.CommandId);

            aMsgIds.Sort();
            aDeliveredMsgIds.Sort();
            Assert.IsTrue(aMsgIds.SequenceEqual(aDeliveredMsgIds));
        }

        [Test]
        public void FailCommand()
        {
            IMessagingSystemFactory anUnderlyingMessaging = new SynchronousMessagingSystemFactory();
            IReliableMessagingFactory aMessagingSystemFactory = new ReliableMessagingFactory(anUnderlyingMessaging, Serializer, TimeSpan.FromMilliseconds(12000));

            IReliableDuplexInputChannel aDuplexInputChannel = aMessagingSystemFactory.CreateDuplexInputChannel("Channel1");
            IReliableDuplexOutputChannel aDuplexOutputChannel = aMessagingSystemFactory.CreateDuplexOutputChannel("Channel1");

            IReliableCommandsFactory aCommandsFactory = new ReliableCommandsFactory(Serializer);

            IReliableCommand<string, int> aCommand = aCommandsFactory.CreateReliableCommand<string, int>(x =>
            {
                // Get input data
                DataFragment<int> anInputData = x.DequeueInputData();

                throw new Exception("Command failed.");

            }, EProcessingStrategy.MultiThread);

            IReliableCommandProxy<string, int> aCommandProxy = aCommandsFactory.CreateReliableCommandProxy<string, int>();

            ManualResetEvent aResponseReceivedEvent = new ManualResetEvent(false);

            CommandResponseReceivedEventArgs<string> aReceivedResponse = null;
            aCommandProxy.CommandResponseReceived += (x, y) =>
            {
                aReceivedResponse = y;
                aResponseReceivedEvent.Set();
            };


            aCommand.AttachReliableInputChannel(aDuplexInputChannel);
            aCommandProxy.AttachReliableOutputChannel(aDuplexOutputChannel);

            aCommandProxy.Execute("MyTag", 100);

            aResponseReceivedEvent.WaitOne();

            Assert.IsNotNull(aReceivedResponse);
            Assert.AreEqual(null, aReceivedResponse.ReturnData);
            Assert.AreEqual("NA", aReceivedResponse.SequenceId);
            Assert.IsTrue(aReceivedResponse.IsSequenceCompleted);
            Assert.AreEqual("Command failed.", aReceivedResponse.CommandError);
            Assert.IsNull(aReceivedResponse.ReceivingError);
            Assert.AreEqual(ECommandState.Failed, aReceivedResponse.CommandState);
            Assert.AreEqual("MyTag", aReceivedResponse.CommandId);
        }

        protected ISerializer Serializer { get; set; }
    }
}
