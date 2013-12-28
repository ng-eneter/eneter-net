/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

#if !SILVERLIGHT && !COMPACT_FRAMEWORK


using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.ConnectionProtocols;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.MessagingSystems.AndroidUsbCableMessagingSystem
{
    internal class AndroidUsbDuplexOutputChannel : IDuplexOutputChannel
    {
        public event EventHandler<DuplexChannelMessageEventArgs> ResponseMessageReceived;
        public event EventHandler<DuplexChannelEventArgs> ConnectionOpened;
        public event EventHandler<DuplexChannelEventArgs> ConnectionClosed;

        public AndroidUsbDuplexOutputChannel(int port, string responseReceiverId, int adbHostPort, IProtocolFormatter<byte[]> protocolFormatter)
        {
            using (EneterTrace.Entering())
            {
                ChannelId = port.ToString();
                ResponseReceiverId = (string.IsNullOrEmpty(responseReceiverId)) ? port + "_" + Guid.NewGuid().ToString() : responseReceiverId;
                myAdbHostPort = adbHostPort;

                IMessagingSystemFactory aTcpMessaging = new TcpMessagingSystemFactory();
                string anIpAddressAndPort = "tcp://127.0.0.1:" + ChannelId + "/";
                myOutputchannel = aTcpMessaging.CreateDuplexOutputChannel(anIpAddressAndPort, ResponseReceiverId);
                myOutputchannel.ConnectionOpened += OnConnectionOpened;
                myOutputchannel.ConnectionClosed += OnConnectionClosed;
                myOutputchannel.ResponseMessageReceived += OnResponseMessageReceived;
            }
        }

        public string ChannelId { get; private set; }

        public string ResponseReceiverId { get; private set; }


        public void OpenConnection()
        {
            using (EneterTrace.Entering())
            {
                // Configure the ADB host to forward communication via the USB cable.
                ConfigureAdbToForwardCommunication(ChannelId);

                // Open connection with the service running on the Android device.
                myOutputchannel.OpenConnection();
            }
        }

        public void CloseConnection()
        {
            using (EneterTrace.Entering())
            {
                // Close connection with the service running on the Android device.
                myOutputchannel.CloseConnection();
            }
        }

        public bool IsConnected
        {
            get
            {
                 return myOutputchannel.IsConnected;
            }
        }

        public void SendMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                myOutputchannel.SendMessage(message);
            }
        }

        public IThreadDispatcher Dispatcher { get { return myOutputchannel.Dispatcher; } }

        private void ConfigureAdbToForwardCommunication(string portNumber)
        {
            using (EneterTrace.Entering())
            {
                string aForwardRequest = string.Format("host:forward:tcp:{0};tcp:{1}", portNumber, portNumber);


                // Open TCP connection with ADB host.
                using (TcpClient aTcpClient = new TcpClient())
                {
                    aTcpClient.Connect(IPAddress.Loopback, myAdbHostPort);

                    // Encode the message for the ADB host.
                    string anAdbRequest = string.Format("{0}{1}\n", aForwardRequest.Length.ToString("X4"), aForwardRequest);
                    byte[] aRequestContent = Encoding.ASCII.GetBytes(anAdbRequest);

                    aTcpClient.GetStream().Write(aRequestContent, 0, aRequestContent.Length);


                    // Read the response from the ADB host.
                    BinaryReader aReader = new BinaryReader(aTcpClient.GetStream());
                    byte[] aResponseContent = aReader.ReadBytes(4);
                    string aResponse = Encoding.ASCII.GetString(aResponseContent);

                    // If ADB response indicates something was wrong.
                    if (aResponse.ToUpper() != "OKAY")
                    {
                        // Try to get the reason why it failed.
                        byte[] aLengthBuf = aReader.ReadBytes(4);
                        string aLengthStr = Encoding.ASCII.GetString(aLengthBuf);

                        int aReasonMessageLength = 0;
                        try
                        {
                            aReasonMessageLength = int.Parse(aLengthStr, System.Globalization.NumberStyles.HexNumber);
                        }
                        catch
                        {
                            EneterTrace.Warning(TracedObject + "failed to parse '" + aLengthStr + "' into a number. A hex format of number was expected.");
                        }

                        string aReasonStr = "";
                        if (aReasonMessageLength > 0)
                        {
                            // Read the content of the error reason message.
                            byte[] aReasonBuf = aReader.ReadBytes(aReasonMessageLength);
                            aReasonStr = Encoding.ASCII.GetString(aReasonBuf);

                        }

                        string anErrorMessage = TracedObject + "failed to configure the ADB host for forwarding the communication. The ADB responded: " + aResponse + " " + aReasonStr;
                        EneterTrace.Error(anErrorMessage);
                        throw new InvalidOperationException(anErrorMessage);
                    }
                }
            }
        }

        private void OnConnectionOpened(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                Notify(ConnectionOpened);
            }
        }

        private void OnConnectionClosed(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                Notify(ConnectionClosed);
            }
        }

        private void OnResponseMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (ResponseMessageReceived != null)
                {
                    try
                    {
                        ResponseMessageReceived(this, new DuplexChannelMessageEventArgs(ChannelId, e.Message, ResponseReceiverId, ""));
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
                else
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.NobodySubscribedForMessage);
                }
            }
        }

        private void Notify(EventHandler<DuplexChannelEventArgs> eventHandler)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    if (eventHandler != null)
                    {
                        DuplexChannelEventArgs aMsg = new DuplexChannelEventArgs(ChannelId, ResponseReceiverId, "");
                        eventHandler(this, aMsg);
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                }
            }
        }


        private IDuplexOutputChannel myOutputchannel;
        private int myAdbHostPort;

        private string TracedObject
        {
            get
            {
                return GetType().Name + " '" + ChannelId + "' ";
            }
        }
    }
}

#endif