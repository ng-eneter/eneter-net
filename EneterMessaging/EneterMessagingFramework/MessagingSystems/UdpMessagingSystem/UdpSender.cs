/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

#if !SILVERLIGHT

using System.Net;
using System.Net.Sockets;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.UdpMessagingSystem
{
    internal class UdpSender
    {
        public UdpSender(Socket socket, EndPoint receiverEndPoint)
        {
            using (EneterTrace.Entering())
            {
                mySocket = socket;
                myReceiverEndpoint = receiverEndPoint;
            }
        }

        public void SendMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                byte[] aMessageData = (byte[])message;
                mySocket.SendTo(aMessageData, myReceiverEndpoint);
            }
        }

        private EndPoint myReceiverEndpoint;
        private Socket mySocket;
    }
}

#endif