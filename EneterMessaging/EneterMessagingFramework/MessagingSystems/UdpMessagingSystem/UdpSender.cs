/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

#if !SILVERLIGHT

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.UdpMessagingSystem
{
    internal class UdpSender : ISender
    {
        public UdpSender(Socket socket, EndPoint receiverEndPoint)
        {
            using (EneterTrace.Entering())
            {
                mySocket = socket;
                myReceiverEndpoint = receiverEndPoint;
            }
        }

        public bool IsStreamWritter { get { return false; } }

        public void SendMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                byte[] aMessageData = (byte[])message;
                mySocket.SendTo(aMessageData, myReceiverEndpoint);
            }
        }

        public void SendMessage(Action<Stream> toStreamWritter)
        {
            throw new NotSupportedException(GetType() + " does not support sending via stream.");
        }

        private EndPoint myReceiverEndpoint;
        private Socket mySocket;
    }
}

#endif