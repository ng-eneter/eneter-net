using Eneter.Messaging.DataProcessing.Serializing;
/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/


using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.EndPoints.TypedMessages;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Eneter.Messaging.Nodes.HolePunching
{
    internal class RendezvousClient : IRendezvousClient
    {
        public RendezvousClient(ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                myRequestTimeout = 10000;

                IDuplexTypedMessagesFactory aFactory = new DuplexTypedMessagesFactory(serializer);
                myRendezvousSender = aFactory.CreateDuplexTypedMessageSender<RendezvousMessage, RendezvousMessage>();
                myRendezvousSender.ResponseReceived += OnResponseReceived;
            }
        }

        public string Register(string rendezvousId)
        {
            using (EneterTrace.Entering())
            {
                if (string.IsNullOrEmpty(rendezvousId))
                {
                    string anErrorMessage = TracedObject + "could not register because input parameter rendezvousId is null or empty string.";
                    EneterTrace.Error(anErrorMessage);
                    throw new ArgumentException(anErrorMessage);
                }

                string aResponse = SendSyncRequest(ERendezvousMessage.RegisterRequest, rendezvousId, ERendezvousMessage.RegisterResponse);
                return aResponse;
            }
        }

        public string GetEndPoint(string rendezvousId)
        {
            using (EneterTrace.Entering())
            {
                if (string.IsNullOrEmpty(rendezvousId))
                {
                    string anErrorMessage = TracedObject + "could not retrieve IP address and port because the input parameter rendezvousId is null or empty string.";
                    EneterTrace.Error(anErrorMessage);
                    throw new ArgumentException(anErrorMessage);
                }

                string aResponse = SendSyncRequest(ERendezvousMessage.GetAddressRequest, rendezvousId, ERendezvousMessage.GetAddressResponse);
                return aResponse;
            }
        }
        

        public void AttachDuplexOutputChannel(IDuplexOutputChannel duplexOutputChannel)
        {
            using (EneterTrace.Entering())
            {
                myRendezvousSender.AttachDuplexOutputChannel(duplexOutputChannel);
            }
        }

        public void DetachDuplexOutputChannel()
        {
            using (EneterTrace.Entering())
            {
                myRendezvousSender.DetachDuplexOutputChannel();
            }
        }

        public bool IsDuplexOutputChannelAttached
        {
            get { return myRendezvousSender.IsDuplexOutputChannelAttached; }
        }

        public IDuplexOutputChannel AttachedDuplexOutputChannel
        {
            get { return myRendezvousSender.AttachedDuplexOutputChannel; }
        }

        private string SendSyncRequest(ERendezvousMessage aRequestType, string messageData, ERendezvousMessage anExpectedResponseType)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(mySyncRequestLock))
                {
                    ManualResetEvent aResponseAvailable = new ManualResetEvent(false);

                    RendezvousMessage aRequest = new RendezvousMessage();
                    aRequest.MessageType = aRequestType;
                    aRequest.MessageData = messageData;

                    RendezvousMessage aResponse = null;
                    EventHandler<TypedResponseReceivedEventArgs<RendezvousMessage>> aResponseMessageHandler =
                        (sender, e) =>
                        {
                            if (e.ResponseMessage != null && e.ResponseMessage.MessageType == anExpectedResponseType)
                            {
                                aResponse = e.ResponseMessage;
                                aResponseAvailable.Set();
                            }
                        };
                    EventHandler<DuplexChannelEventArgs> aConnectionClosedHandler =
                        (sender, e) =>
                        {
                            aResponseAvailable.Set();
                        };
                    myRendezvousSender.ResponseReceived += aResponseMessageHandler;
                    myRendezvousSender.ConnectionClosed += aConnectionClosedHandler;

                    try
                    {
                        myRendezvousSender.SendRequestMessage(aRequest);

                        if (!aResponseAvailable.WaitOne(myRequestTimeout))
                        {
                            string anErrorMessage = TracedObject + "failed to register in Rendezvous service within " + myRequestTimeout + " ms.";
                            EneterTrace.Error(anErrorMessage);
                            throw new TimeoutException(anErrorMessage);
                        }
                    }
                    finally
                    {
                        myRendezvousSender.ResponseReceived -= aResponseMessageHandler;
                        myRendezvousSender.ConnectionClosed -= aConnectionClosedHandler;
                    }

                    // If response data does not exist.
                    if (aResponse == null)
                    {
                        string anErrorMessage = TracedObject + "failed to receive the response.";

                        IDuplexOutputChannel anAttachedOutputChannel = myRendezvousSender.AttachedDuplexOutputChannel;
                        if (anAttachedOutputChannel == null)
                        {
                            anErrorMessage += " The duplex outputchannel was detached.";
                        }
                        else if (!anAttachedOutputChannel.IsConnected)
                        {
                            anErrorMessage += " The connection was closed.";
                        }

                        EneterTrace.Error(anErrorMessage);
                        throw new InvalidOperationException(anErrorMessage);
                    }

                    return aResponse.MessageData;
                }
            }
        }

        private void OnResponseReceived(object sender, TypedResponseReceivedEventArgs<RendezvousMessage> e)
        {
            using (EneterTrace.Entering())
            {
                Console.WriteLine("OnResponseReceived");

                ThreadPool.QueueUserWorkItem(x =>
                    {
                        if (e.ReceivingError == null && e.ResponseMessage.MessageType == ERendezvousMessage.Drill)
                        {
                            string anIpAddressAndPort = "tcp://" + e.ResponseMessage.MessageData + "/";
                            Console.WriteLine("Drilling to '" + anIpAddressAndPort + "'.");

                            Uri aUri = new Uri(anIpAddressAndPort);
                            Socket aTcpClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                            //TcpClient aTcpClient = new TcpClient(anAddressFamily);
                            aTcpClient.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                            // Set the response receiving port.
                            aTcpClient.Bind(new IPEndPoint(IPAddress.Any, 8085));
                            Console.WriteLine("Bound to 8085.");

                            //for (int i = 0; i < 10; ++i)
                            {
                                try
                                {
                                    Console.WriteLine("Connecting ... ");

                                    // This call also resolves host names.
                                    //aTcpClient.Connect(aUri.Host, aUri.Port);
                                    aTcpClient.Connect(new IPEndPoint(IPAddress.Parse(aUri.Host), aUri.Port));
                                }
                                catch (Exception err)
                                {
                                    Console.WriteLine(err.Message);
                                }

                            }

                            Console.WriteLine("Leaving ... ");
                            //aTcpClient.Close();
                        }
                    });
            }
        }

        private int myRequestTimeout;
        private object mySyncRequestLock = new object();
        private IDuplexTypedMessageSender<RendezvousMessage, RendezvousMessage> myRendezvousSender;

        private string TracedObject { get { return GetType().Name + " "; } }
    }
}
