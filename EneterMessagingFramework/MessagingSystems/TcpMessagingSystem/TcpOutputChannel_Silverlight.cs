/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if SILVERLIGHT && !WINDOWS_PHONE

using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.Diagnostic;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Threading;
using Eneter.Messaging.DataProcessing.Streaming;

namespace Eneter.Messaging.MessagingSystems.TcpMessagingSystem
{
    internal class TcpOutputChannel_Silverlight : IOutputChannel
    {
        public TcpOutputChannel_Silverlight(string ipAddressAndPort, int sendMessageTimeout)
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

                ChannelId = ipAddressAndPort;
                mySendMessageTimeout = sendMessageTimeout;
            }
        }


        public string ChannelId { get; private set; }


        public void SendMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                // 4502 and 4532 ???
                //DnsEndPoint anEndPoint = new DnsEndPoint(Application.Current.Host.Source.DnsSafeHost, 4530);
                DnsEndPoint anEndPoint = new DnsEndPoint(myUriBuilder.Host, myUriBuilder.Port);
                Socket aSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                string anError = "";

                AutoResetEvent aCompletedSignal = new AutoResetEvent(false);

                try
                {
                    SocketAsyncEventArgs aSocketAsyncEventArgs = new SocketAsyncEventArgs();
                    aSocketAsyncEventArgs.UserToken = aSocket;
                    aSocketAsyncEventArgs.RemoteEndPoint = anEndPoint;
                    aSocketAsyncEventArgs.Completed += (x, y) =>
                        {
                            using (EneterTrace.Entering())
                            {
                                if (y.SocketError == SocketError.Success)
                                {
                                    // Store the message in the buffer
                                    byte[] aBufferedMessage = null;
                                    using (MemoryStream aMemStream = new MemoryStream())
                                    {
                                        MessageStreamer.WriteMessage(aMemStream, message);
                                        aBufferedMessage = aMemStream.ToArray();
                                    }

                                    SocketAsyncEventArgs anSocketAsyncEventArgs = new SocketAsyncEventArgs();
                                    anSocketAsyncEventArgs.UserToken = aSocket;
                                    anSocketAsyncEventArgs.RemoteEndPoint = anEndPoint;
                                    anSocketAsyncEventArgs.SetBuffer(aBufferedMessage, 0, aBufferedMessage.Length);
                                    anSocketAsyncEventArgs.Completed += (xx, yy) =>
                                        {
                                            using (EneterTrace.Entering())
                                            {
                                                if (yy.SocketError != SocketError.Success)
                                                {
                                                    anError = TracedObject + "failed to send the message because " + y.SocketError.ToString();
                                                }

                                                // Indicate the message is sent
                                                aCompletedSignal.Set();
                                            }
                                        };
                                    aSocket.SendAsync(anSocketAsyncEventArgs);
                                }
                                else
                                {
                                    anError = TracedObject + "failed to send the message because " + y.SocketError.ToString();

                                    // Indicate that we are done.
                                    aCompletedSignal.Set();
                                }
                            }
                        };

                    aSocket.ConnectAsync(aSocketAsyncEventArgs);
                }
                catch (Exception err)
                {
                    anError = TracedObject + "failed to send the message because:" + err.Message + "\r\n" + err.StackTrace;
                    aCompletedSignal.Set();
                }


                if (!aCompletedSignal.WaitOne(mySendMessageTimeout))
                {
                    anError = TracedObject + "failed to send the message within a specified timeout.";
                }

                aSocket.Close();

                if (anError != "")
                {
                    EneterTrace.Error(anError);
                    throw new InvalidOperationException(anError);
                }
            }
        }


        private UriBuilder myUriBuilder;

        private int mySendMessageTimeout;

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
