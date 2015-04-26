/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/

#if WINDOWS_PHONE80 || WINDOWS_PHONE81

using Eneter.Messaging.Diagnostic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Eneter.Messaging.MessagingSystems.TcpMessagingSystem
{
    internal class TcpListener
    {
        public TcpListener(IPEndPoint address)
        {
            using (EneterTrace.Entering())
            {
                myAddress = address;
            }
        }

        public void Start()
        {
            using (EneterTrace.Entering())
            {
                if (myServiceSocket != null)
                {
                    return;
                }

                myServiceSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                myServiceSocket.Bind(myAddress);

                myServiceSocket.Listen(int.MaxValue);
            }
        }

        public void Stop()
        {
            using (EneterTrace.Entering())
            {
                if (myServiceSocket != null)
                {
                    myServiceSocket.Close();
                    myServiceSocket = null;
                }
            }
        }

        public TcpClient AcceptTcpClient()
        {
            using (EneterTrace.Entering())
            {
                ManualResetEvent aClientConnectedEvent = new ManualResetEvent(false);
                SocketAsyncEventArgs anAcceptSocketEventArgs = new SocketAsyncEventArgs();
                anAcceptSocketEventArgs.Completed += (x, y) => aClientConnectedEvent.Set();

                if (myServiceSocket.AcceptAsync(anAcceptSocketEventArgs))
                {
                    // the completion is pending so wait until completed.
                    aClientConnectedEvent.WaitOne();
                }

                TcpClient anAcceptedClient = new TcpClient(anAcceptSocketEventArgs.AcceptSocket);
                return anAcceptedClient;
            }
        }

        private IPEndPoint myAddress;
        private Socket myServiceSocket;
    }
}


#endif