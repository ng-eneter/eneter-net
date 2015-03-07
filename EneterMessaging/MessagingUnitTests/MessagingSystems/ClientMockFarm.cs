using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using System.Threading;
using Eneter.Messaging.Diagnostic;

namespace Eneter.MessagingUnitTests.MessagingSystems
{
    internal class ClientMockFarm
    {
        public ClientMockFarm(IMessagingSystemFactory messaging, string channelId, int numberOfClients)
        {
            for (int i = 0; i < numberOfClients; ++i)
            {
                ClientMock aClient = new ClientMock(messaging, channelId);
                myClients.Add(aClient);
            }
        }

        public void ClearTestResults()
        {
            foreach (ClientMock aClient in myClients)
            {
                aClient.ClearTestResults();
            }
        }

        public void OpenConnectionsAsync()
        {
            foreach (ClientMock aClient in myClients)
            {
                ClientMock aC = aClient;
                WaitCallback aW = x =>
                {
                    //EneterTrace.Info("CONNECT CLIENT");
                    aC.OutputChannel.OpenConnection();
                };
                ThreadPool.QueueUserWorkItem(aW);

                Thread.Sleep(2);
            }
        }

        public void DoOnConnectionOpen(Action<object, DuplexChannelEventArgs> doOnConnectionOpen)
        {
            foreach (ClientMock aClient in myClients)
            {
                aClient.DoOnConnectionOpen(doOnConnectionOpen);
            }
        }
        public void DoOnConnectionOpen_CloseConnection()
        {
            foreach (ClientMock aClient in myClients)
            {
                aClient.DoOnConnectionOpen_CloseConnection();
            }
        }

        public void WaitUntilAllConnectionsAreOpen(int milliseconds)
        {
            foreach (ClientMock aClient in myClients)
            {
                aClient.WaitUntilConnectionOpenIsNotified(milliseconds);
            }
        }

        public void CloseAllConnections()
        {
            foreach (ClientMock aClient in myClients)
            {
                aClient.OutputChannel.CloseConnection();
            }
        }

        public void DoOnConnectionClosed(Action<object, DuplexChannelEventArgs> doOnConnectionClosed)
        {
            foreach (ClientMock aClient in myClients)
            {
                aClient.DoOnConnectionClosed(doOnConnectionClosed);
            }
        }
        public void DoOnConnectionClosed_OpenConnection()
        {
            foreach (ClientMock aClient in myClients)
            {
                aClient.DoOnConnectionClosed_OpenConnection();
            }
        }

        public void SendMessageAsync(object message, int repeat)
        {
            foreach (ClientMock aClient in myClients)
            {
                ClientMock aC = aClient;
                WaitCallback aW = x =>
                {
                    for (int i = 0; i < repeat; ++i)
                    {
                        aC.OutputChannel.SendMessage(message);
                    }
                };
                ThreadPool.QueueUserWorkItem(aW);

                Thread.Sleep(2);
            }
        }


        public void WaitUntilAllResponsesAreReceived(int numberOfExpectedResponseMessagesPerClient, int milliseconds)
        {
            using (EneterTrace.Entering())
            {
                int anOverallNumberOfExpectedResponseMessages = numberOfExpectedResponseMessagesPerClient * myClients.Count;

                try
                {
                    foreach (ClientMock aClient in myClients)
                    {
                        aClient.WaitUntilResponseMessagesAreReceived(numberOfExpectedResponseMessagesPerClient, milliseconds);
                    }
                }
                catch
                {
                    EneterTrace.Error("Received responses: " + ReceivedResponses.Count() + " Expected: " + anOverallNumberOfExpectedResponseMessages);
                    throw;
                }
            }
        }

        public IEnumerable<ClientMock> Clients { get { return myClients; } }
        public IEnumerable<DuplexChannelMessageEventArgs> ReceivedResponses
        {
            get
            {
                return myClients.SelectMany(x => x.ReceivedMessages);
            }
        }


        private List<ClientMock> myClients = new List<ClientMock>();
    }
}
