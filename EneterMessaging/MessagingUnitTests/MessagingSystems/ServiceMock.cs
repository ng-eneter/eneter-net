using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using System.Threading;
using Eneter.Messaging.Diagnostic;

namespace Eneter.MessagingUnitTests.MessagingSystems
{
    internal class ServiceMock
    {
        public ServiceMock(IMessagingSystemFactory messaging, string channelId)
        {
            InputChannel = messaging.CreateDuplexInputChannel(channelId);
            InputChannel.ResponseReceiverConnected += OnResponseReceiverConnected;
            InputChannel.ResponseReceiverDisconnected += OnResponseReceiverDisconnected;
            InputChannel.MessageReceived += OnMessageReceived;
        }

        public void ClearTestResults()
        {
            myResponseReceiversAreConnectedEvent.Reset();
            myAllResponseReceiversDisconnectedEvent.Reset();
            myRequestMessagesReceivedEvent.Reset();

            myDisconnectedResponseReceivers.Clear();
            myReceivedMessages.Clear();
        }

        public IDuplexInputChannel InputChannel { get; private set; }

        public void WaitUntilResponseReceiversAreConnected(int numberOfExpectedReceivers, int milliseconds)
        {
            lock (myConnectedResponseReceivers)
            {
                myNumberOfExpectedReceivers = numberOfExpectedReceivers;
                if (numberOfExpectedReceivers == myConnectedResponseReceivers.Count)
                {
                    myResponseReceiversAreConnectedEvent.Set();
                }
                else
                {
                    myResponseReceiversAreConnectedEvent.Reset();
                }
            }

            myResponseReceiversAreConnectedEvent.WaitIfNotDebugging(milliseconds);
        }

        public IEnumerable<ResponseReceiverEventArgs> ConnectedResponseReceivers { get { return myConnectedResponseReceivers; } }

        public void WaitUntilAllResponseReceiversAreDisconnected(int milliseconds)
        {
            myAllResponseReceiversDisconnectedEvent.WaitIfNotDebugging(milliseconds);
        }

        public IEnumerable<ResponseReceiverEventArgs> DisconnectedResponseReceivers { get { return myDisconnectedResponseReceivers; } }

        

        public void WaitUntilMessagesAreReceived(int numberOfExpectedMessages, int milliseconds)
        {
            lock (myReceivedMessages)
            {
                myNumberOfExpectedRequestMessages = numberOfExpectedMessages;
                if (myReceivedMessages.Count == myNumberOfExpectedRequestMessages)
                {
                    myRequestMessagesReceivedEvent.Set();
                }
                else
                {
                    myRequestMessagesReceivedEvent.Reset();
                }
            }

            myRequestMessagesReceivedEvent.WaitIfNotDebugging(milliseconds);
        }

        public void DoOnMessageReceived(Action<object, DuplexChannelMessageEventArgs> doOnMessageReceived)
        {
            myDoOnMessageReceived = doOnMessageReceived;
        }
        public void DoOnMessageReceived_SendResponse(object responseMessage)
        {
            myDoOnMessageReceived = (x, y) => InputChannel.SendResponseMessage(y.ResponseReceiverId, responseMessage);
        }

        public IEnumerable<DuplexChannelMessageEventArgs> ReceivedMessages { get { return myReceivedMessages; } }


        private void OnResponseReceiverConnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectedResponseReceivers)
                {
                    myConnectedResponseReceivers.Add(e);

                    if (myConnectedResponseReceivers.Count == myNumberOfExpectedReceivers)
                    {
                        myResponseReceiversAreConnectedEvent.Set();
                    }
                }
            }
        }

        private void OnResponseReceiverDisconnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectedResponseReceivers)
                {
                    myConnectedResponseReceivers.RemoveAll(x => x.ResponseReceiverId == e.ResponseReceiverId);
                    myDisconnectedResponseReceivers.Add(e);

                    if (myConnectedResponseReceivers.Count == 0)
                    {
                        myAllResponseReceiversDisconnectedEvent.Set();
                    }
                }
            }
        }

        private void OnMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                lock (myReceivedMessages)
                {
                    myReceivedMessages.Add(e);

                    if (myReceivedMessages.Count == myNumberOfExpectedRequestMessages)
                    {
                        myRequestMessagesReceivedEvent.Set();
                    }

                    if (myDoOnMessageReceived != null)
                    {
                        myDoOnMessageReceived(sender, e);
                    }
                }
            }
        }



        private int myNumberOfExpectedReceivers;
        private ManualResetEvent myResponseReceiversAreConnectedEvent = new ManualResetEvent(false);
        private List<ResponseReceiverEventArgs> myConnectedResponseReceivers = new List<ResponseReceiverEventArgs>();

        private ManualResetEvent myAllResponseReceiversDisconnectedEvent = new ManualResetEvent(false);
        private List<ResponseReceiverEventArgs> myDisconnectedResponseReceivers = new List<ResponseReceiverEventArgs>();

        private ManualResetEvent myRequestMessagesReceivedEvent = new ManualResetEvent(false);
        private List<DuplexChannelMessageEventArgs> myReceivedMessages = new List<DuplexChannelMessageEventArgs>();
        private int myNumberOfExpectedRequestMessages;
        private Action<object, DuplexChannelMessageEventArgs> myDoOnMessageReceived;
    }
}
