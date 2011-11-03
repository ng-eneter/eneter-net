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

namespace Eneter.MessagingUnitTests.EndPoints.Commands
{
    public abstract class CommandTesterBase
    {
        [Test]
        public void ConnectDisconnectCommand()
        {
            IMessagingSystemFactory aMessagingSystemFactory = new SynchronousMessagingSystemFactory();

            IDuplexInputChannel aDuplexInputChannel = aMessagingSystemFactory.CreateDuplexInputChannel("Channel1");
            IDuplexOutputChannel aDuplexOutputChannel = aMessagingSystemFactory.CreateDuplexOutputChannel("Channel1");

            ICommandsFactory aCommandsFactory = new CommandsFactory(Serializer);

            ICommand<string, int> aCommand = aCommandsFactory.CreateCommand<string, int>(x => { }, EProcessingStrategy.MultiThread);

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

            ICommandProxy<string, int> aCommandProxy = aCommandsFactory.CreateCommandProxy<string, int>();

            aCommand.AttachDuplexInputChannel(aDuplexInputChannel);
            aCommandProxy.AttachDuplexOutputChannel(aDuplexOutputChannel);

            Assert.AreNotEqual("", aConnectedProxyId);

            aCommandProxy.DetachDuplexOutputChannel();

            Assert.AreNotEqual("", aDisconnectedProxyId);
        }

        [Test]
        public void ExecuteCommand()
        {
            IMessagingSystemFactory aMessagingSystemFactory = new SynchronousMessagingSystemFactory();

            IDuplexInputChannel aDuplexInputChannel = aMessagingSystemFactory.CreateDuplexInputChannel("Channel1");
            IDuplexOutputChannel aDuplexOutputChannel = aMessagingSystemFactory.CreateDuplexOutputChannel("Channel1");

            ICommandsFactory aCommandsFactory = new CommandsFactory(Serializer);

            ICommand<string, int> aCommand = aCommandsFactory.CreateCommand<string, int>(x =>
                {
                    DataFragment<int> anInputData = x.DequeueInputData();
                    string aResponse = anInputData.Data.ToString();

                    x.Response(ECommandState.Completed, aResponse);
                }, EProcessingStrategy.MultiThread);

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

            ICommandProxy<string, int> aCommandProxy = aCommandsFactory.CreateCommandProxy<string, int>();

            ManualResetEvent aResponseReceivedEvent = new ManualResetEvent(false);

            CommandResponseReceivedEventArgs<string> aReceivedResponse = null;
            aCommandProxy.CommandResponseReceived += (x, y) =>
                {
                    aReceivedResponse = y;

                    aResponseReceivedEvent.Set();
                };


            aCommand.AttachDuplexInputChannel(aDuplexInputChannel);
            aCommandProxy.AttachDuplexOutputChannel(aDuplexOutputChannel);

            Assert.AreNotEqual("", aConnectedProxyId);

            aCommandProxy.Execute("MyTag", 100);

            aResponseReceivedEvent.WaitOne();

            Assert.IsNotNull(aReceivedResponse);
            Assert.AreEqual("100", aReceivedResponse.ReturnData);
            Assert.AreEqual("NA", aReceivedResponse.SequenceId);
            Assert.IsTrue(aReceivedResponse.IsSequenceCompleted);
            Assert.AreEqual("", aReceivedResponse.CommandError);
            Assert.IsNull(aReceivedResponse.ReceivingError);
            Assert.AreEqual(ECommandState.Completed, aReceivedResponse.CommandState);
            Assert.AreEqual("MyTag", aReceivedResponse.CommandId);
        }

        [Test]
        public void ExecuteDisconnectCommand()
        {
            IMessagingSystemFactory aMessagingSystemFactory = new SynchronousMessagingSystemFactory();

            IDuplexInputChannel aDuplexInputChannel = aMessagingSystemFactory.CreateDuplexInputChannel("Channel1");
            IDuplexOutputChannel aDuplexOutputChannel = aMessagingSystemFactory.CreateDuplexOutputChannel("Channel1");

            ICommandsFactory aCommandsFactory = new CommandsFactory(Serializer);

            ManualResetEvent aCommandProcessedEvent = new ManualResetEvent(false);

            bool wasDisconnected = false;
            ICommand<string, int> aCommand = aCommandsFactory.CreateCommand<string, int>(x =>
            {
                try
                {
                    // Get input data
                    DataFragment<int> anInputData = x.DequeueInputData();

                    for (int i = 0; i < 100; ++i)
                    {
                        Thread.Sleep(10);

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

            ICommandProxy<string, int> aCommandProxy = aCommandsFactory.CreateCommandProxy<string, int>();

            aCommand.AttachDuplexInputChannel(aDuplexInputChannel);
            aCommandProxy.AttachDuplexOutputChannel(aDuplexOutputChannel);

            aCommandProxy.Execute("MyTag", 100);

            aCommandProxy.DetachDuplexOutputChannel();

            aCommandProcessedEvent.WaitOne();

            Assert.IsTrue(wasDisconnected);
        }

        [Test]
        public void ExecuteMultipleCommandsMultiThread()
        {
            IMessagingSystemFactory aMessagingSystemFactory = new SynchronousMessagingSystemFactory();

            IDuplexInputChannel aDuplexInputChannel = aMessagingSystemFactory.CreateDuplexInputChannel("Channel1");
            IDuplexOutputChannel aDuplexOutputChannel = aMessagingSystemFactory.CreateDuplexOutputChannel("Channel1");

            ICommandsFactory aCommandsFactory = new CommandsFactory(Serializer);

            ICommand<string, int> aCommand = aCommandsFactory.CreateCommand<string, int>(x =>
            {
                DataFragment<int> anInputData = x.DequeueInputData();

                for (int i = 0; i < 100; ++i)
                {
                    Thread.Sleep(10);
                }
                
                string aResponse = anInputData.Data.ToString();
                x.Response(ECommandState.Completed, aResponse);
            }, EProcessingStrategy.MultiThread);


            ICommandProxy<string, int> aCommandProxy = aCommandsFactory.CreateCommandProxy<string, int>();

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


            aCommand.AttachDuplexInputChannel(aDuplexInputChannel);
            aCommandProxy.AttachDuplexOutputChannel(aDuplexOutputChannel);

            aCommandProxy.Execute("CommandId1", 100);
            aCommandProxy.Execute("CommandId2", 200);
            aCommandProxy.Execute("CommandId3", 300);

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
        }

        [Test]
        public void ExecuteMultipleCommandsSingleThread()
        {
            IMessagingSystemFactory aMessagingSystemFactory = new SynchronousMessagingSystemFactory();

            IDuplexInputChannel aDuplexInputChannel = aMessagingSystemFactory.CreateDuplexInputChannel("Channel1");
            IDuplexOutputChannel aDuplexOutputChannel = aMessagingSystemFactory.CreateDuplexOutputChannel("Channel1");

            ICommandsFactory aCommandsFactory = new CommandsFactory(Serializer);

            ICommand<string, int> aCommand = aCommandsFactory.CreateCommand<string, int>(x =>
            {
                DataFragment<int> anInputData = x.DequeueInputData();

                for (int i = 0; i < 100; ++i)
                {
                    Thread.Sleep(10);
                }

                string aResponse = anInputData.Data.ToString();
                x.Response(ECommandState.Completed, aResponse);
            }, EProcessingStrategy.SingleThread);


            ICommandProxy<string, int> aCommandProxy = aCommandsFactory.CreateCommandProxy<string, int>();

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


            aCommand.AttachDuplexInputChannel(aDuplexInputChannel);
            aCommandProxy.AttachDuplexOutputChannel(aDuplexOutputChannel);

            try
            {
                aCommandProxy.Execute("CommandId1", 100);
                aCommandProxy.Execute("CommandId2", 200);
                aCommandProxy.Execute("CommandId3", 300);

                aResponseReceivedEvent.WaitOne();
            }
            finally
            {
                aCommandProxy.DetachDuplexOutputChannel();
            }

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
        }


        [Test]
        public void PauseResumeCommand()
        {
            IMessagingSystemFactory aMessagingSystemFactory = new SynchronousMessagingSystemFactory();

            IDuplexInputChannel aDuplexInputChannel = aMessagingSystemFactory.CreateDuplexInputChannel("Channel1");
            IDuplexOutputChannel aDuplexOutputChannel = aMessagingSystemFactory.CreateDuplexOutputChannel("Channel1");

            ICommandsFactory aCommandsFactory = new CommandsFactory(Serializer);

            bool wasPaused = false;
            ICommand<string, int> aCommand = aCommandsFactory.CreateCommand<string, int>(x =>
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

            ICommandProxy<string, int> aCommandProxy = aCommandsFactory.CreateCommandProxy<string, int>();

            ManualResetEvent aResponseReceivedEvent = new ManualResetEvent(false);

            CommandResponseReceivedEventArgs<string> aReceivedResponse = null;
            aCommandProxy.CommandResponseReceived += (x, y) =>
            {
                aReceivedResponse = y;
                aResponseReceivedEvent.Set();
            };


            aCommand.AttachDuplexInputChannel(aDuplexInputChannel);
            aCommandProxy.AttachDuplexOutputChannel(aDuplexOutputChannel);

            aCommandProxy.Execute("MyTag", 100);
            
            aCommandProxy.Pause("MyTag");
            Thread.Sleep(500);
            aCommandProxy.Resume("MyTag");

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
        }

        [Test]
        public void PauseCancelCommand()
        {
            IMessagingSystemFactory aMessagingSystemFactory = new SynchronousMessagingSystemFactory();

            IDuplexInputChannel aDuplexInputChannel = aMessagingSystemFactory.CreateDuplexInputChannel("Channel1");
            IDuplexOutputChannel aDuplexOutputChannel = aMessagingSystemFactory.CreateDuplexOutputChannel("Channel1");

            ICommandsFactory aCommandsFactory = new CommandsFactory(Serializer);

            bool wasPaused = false;
            bool wasCanceled = false;
            ICommand<string, int> aCommand = aCommandsFactory.CreateCommand<string, int>(x =>
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

            ICommandProxy<string, int> aCommandProxy = aCommandsFactory.CreateCommandProxy<string, int>();

            ManualResetEvent aResponseReceivedEvent = new ManualResetEvent(false);

            CommandResponseReceivedEventArgs<string> aReceivedResponse = null;
            aCommandProxy.CommandResponseReceived += (x, y) =>
            {
                aReceivedResponse = y;
                aResponseReceivedEvent.Set();
            };


            aCommand.AttachDuplexInputChannel(aDuplexInputChannel);
            aCommandProxy.AttachDuplexOutputChannel(aDuplexOutputChannel);

            aCommandProxy.Execute("MyTag", 100);

            aCommandProxy.Pause("MyTag");
            Thread.Sleep(500);
            aCommandProxy.Cancel("MyTag");

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
        }

        [Test]
        public void FailCommand()
        {
            IMessagingSystemFactory aMessagingSystemFactory = new SynchronousMessagingSystemFactory();

            IDuplexInputChannel aDuplexInputChannel = aMessagingSystemFactory.CreateDuplexInputChannel("Channel1");
            IDuplexOutputChannel aDuplexOutputChannel = aMessagingSystemFactory.CreateDuplexOutputChannel("Channel1");

            ICommandsFactory aCommandsFactory = new CommandsFactory(Serializer);

            ICommand<string, int> aCommand = aCommandsFactory.CreateCommand<string, int>(x =>
            {
                // Get input data
                DataFragment<int> anInputData = x.DequeueInputData();

                throw new Exception("Command failed.");

            }, EProcessingStrategy.MultiThread);

            ICommandProxy<string, int> aCommandProxy = aCommandsFactory.CreateCommandProxy<string, int>();

            ManualResetEvent aResponseReceivedEvent = new ManualResetEvent(false);

            CommandResponseReceivedEventArgs<string> aReceivedResponse = null;
            aCommandProxy.CommandResponseReceived += (x, y) =>
            {
                aReceivedResponse = y;
                aResponseReceivedEvent.Set();
            };


            aCommand.AttachDuplexInputChannel(aDuplexInputChannel);
            aCommandProxy.AttachDuplexOutputChannel(aDuplexOutputChannel);

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
