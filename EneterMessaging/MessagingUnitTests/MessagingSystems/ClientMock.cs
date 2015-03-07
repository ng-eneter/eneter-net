using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using System.Diagnostics;
using System.Threading;
using Eneter.Messaging.Diagnostic;

namespace Eneter.MessagingUnitTests.MessagingSystems
{
    internal class ClientMock
    {
        public ClientMock(IMessagingSystemFactory messaging, string channelId)
            : this(messaging.CreateDuplexOutputChannel(channelId))
        {
        }

        public ClientMock(IDuplexOutputChannel outputChannel)
        {
            OutputChannel = outputChannel;
            OutputChannel.ConnectionOpened += OnConnectionOpened;
            OutputChannel.ConnectionClosed += OnConnectionClosed;
            OutputChannel.ResponseMessageReceived += OnResponseMessageReceived;
        }

        public void ClearTestResults()
        {
            myConnectionOpenIsNotifiedEvent.Reset();
            myConnectionClosedIsNotifiedEvent.Reset();
            myResponseMessagesReceivedEvent.Reset();

            myReceivedMessages.Clear();

            NotifiedOpenConnection = null;
            NotifiedCloseConnection = null;
        }

        public void WaitUntilConnectionOpenIsNotified(int milliseconds)
        {
            using (EneterTrace.Entering())
            {
                myConnectionOpenIsNotifiedEvent.WaitIfNotDebugging(milliseconds);
            }
        }

        public void DoOnConnectionOpen(Action<object, DuplexChannelEventArgs> doOnConnectionOpen)
        {
            myDoOnConnectionOpen = doOnConnectionOpen;
        }
        public void DoOnConnectionOpen_CloseConnection()
        {
            myDoOnConnectionOpen = (x, y) => OutputChannel.CloseConnection();
        }

        public DuplexChannelEventArgs NotifiedOpenConnection { get; private set; }

        



        public void WaitUntilConnectionClosedIsNotified(int milliseconds)
        {
            using (EneterTrace.Entering())
            {
                myConnectionClosedIsNotifiedEvent.WaitIfNotDebugging(milliseconds);
            }
        }

        public void DoOnConnectionClosed(Action<object, DuplexChannelEventArgs> doOnConnectionClosed)
        {
            myDoOnConnectionClosed = doOnConnectionClosed;
        }
        public void DoOnConnectionClosed_OpenConnection()
        {
            myDoOnConnectionClosed = (x, y) => OutputChannel.OpenConnection();
        }

        public DuplexChannelEventArgs NotifiedCloseConnection { get; private set; }

        
        public void WaitUntilResponseMessagesAreReceived(int numberOfExpectedResonseMessages, int milliseconds)
        {
            lock (myReceivedMessages)
            {
                myNumberOfExpectedResponseMessages = numberOfExpectedResonseMessages;
                if (myReceivedMessages.Count == myNumberOfExpectedResponseMessages)
                {
                    myResponseMessagesReceivedEvent.Set();
                }
                else
                {
                    myResponseMessagesReceivedEvent.Reset();
                }
            }
            myResponseMessagesReceivedEvent.WaitIfNotDebugging(milliseconds);
        }

        public IEnumerable<DuplexChannelMessageEventArgs> ReceivedMessages { get { return myReceivedMessages; } }



        private void OnConnectionOpened(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                NotifiedOpenConnection = e;
                myConnectionOpenIsNotifiedEvent.Set();
                if (myDoOnConnectionOpen != null)
                {
                    myDoOnConnectionOpen(sender, e);
                }
            }
        }

        private void OnConnectionClosed(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                NotifiedCloseConnection = e;
                myConnectionClosedIsNotifiedEvent.Set();
                if (myDoOnConnectionClosed != null)
                {
                    myDoOnConnectionClosed(sender, e);
                }
            }
        }

        private void OnResponseMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                lock (myReceivedMessages)
                {
                    myReceivedMessages.Add(e);
                    if (myReceivedMessages.Count == myNumberOfExpectedResponseMessages)
                    {
                        myResponseMessagesReceivedEvent.Set();
                    }
                }
            }
        }


        public IDuplexOutputChannel OutputChannel { get; private set; }

        private ManualResetEvent myConnectionOpenIsNotifiedEvent = new ManualResetEvent(false);
        private Action<object, DuplexChannelEventArgs> myDoOnConnectionOpen;

        private ManualResetEvent myConnectionClosedIsNotifiedEvent = new ManualResetEvent(false);
        private Action<object, DuplexChannelEventArgs> myDoOnConnectionClosed;

        private ManualResetEvent myResponseMessagesReceivedEvent = new ManualResetEvent(false);
        private List<DuplexChannelMessageEventArgs> myReceivedMessages = new List<DuplexChannelMessageEventArgs>();
        private int myNumberOfExpectedResponseMessages;
    }
}
