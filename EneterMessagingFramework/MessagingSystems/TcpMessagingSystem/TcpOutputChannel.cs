/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/


#if !SILVERLIGHT

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using Eneter.Messaging.DataProcessing.Streaming;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security;

namespace Eneter.Messaging.MessagingSystems.TcpMessagingSystem
{
    internal class TcpOutputChannel : IOutputChannel
    {
        public TcpOutputChannel(string ipAddressAndPort, ISecurityFactory serverSecurityFactory)
        {
            using (EneterTrace.Entering())
            {
                if (string.IsNullOrEmpty(ipAddressAndPort))
                {
                    EneterTrace.Error(ErrorHandler.NullOrEmptyChannelId);
                    throw new ArgumentException(ErrorHandler.NullOrEmptyChannelId);
                }

                try
                {
                    // just check if the address is valid
                    myUriBuilder = new UriBuilder(ipAddressAndPort);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.InvalidUriAddress, err);
                    throw;
                }

                mySecurityStreamFactory = serverSecurityFactory;

                ChannelId = ipAddressAndPort;
            }
        }

        public string ChannelId { get; private set; }

        public void SendMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                lock (this)
                {
                    TcpClient aTcpClient = new TcpClient();

                    try
                    {
                        aTcpClient.Connect(IPAddress.Parse(myUriBuilder.Host), myUriBuilder.Port);

                        // Store the message in the buffer
                        byte[] aBufferedMessage = null;
                        using (MemoryStream aMemStream = new MemoryStream())
                        {
                            MessageStreamer.WriteMessage(aMemStream, message);
                            aBufferedMessage = aMemStream.ToArray();
                        }

                        Stream aSendStream;
                        if (mySecurityStreamFactory != null)
                        {
                            aSendStream = mySecurityStreamFactory.CreateSecurityStreamAndAuthenticate(aTcpClient.GetStream());
                        }
                        else
                        {
                            aSendStream = aTcpClient.GetStream();
                        }

                        // Send the message from the buffer.
                        aSendStream.Write(aBufferedMessage, 0, aBufferedMessage.Length);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.SendMessageFailure, err);
                        throw;
                    }
                    finally
                    {
                        aTcpClient.Close();
                    }
                }
            }
        }

        private UriBuilder myUriBuilder;
        private ISecurityFactory mySecurityStreamFactory;

        private string TracedObject
        {
            get 
            {
                return "The Tcp output channel '" + ChannelId + "' "; 
            }
        }
    }
}

#endif