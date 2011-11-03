/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/


#if !SILVERLIGHT

using System;
using System.IO;
using System.Net.Sockets;
using Eneter.Messaging.DataProcessing.Streaming;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using System.Collections.Generic;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security;

namespace Eneter.Messaging.MessagingSystems.TcpMessagingSystem
{
    internal class TcpInputChannel : TcpInputChannelBase, IInputChannel
    {
        public event EventHandler<ChannelMessageEventArgs> MessageReceived;

        public TcpInputChannel(string ipAddressAndPort, ISecurityFactory securityStreamFactory)
            : base(ipAddressAndPort, securityStreamFactory)
        {
            using (EneterTrace.Entering())
            {
            }
        }


        protected override void DisconnectClients()
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectedSenders)
                {
                    foreach (TcpClient aConnection in myConnectedSenders)
                    {
                        aConnection.Close();
                    }
                    myConnectedSenders.Clear();
                }
            }
        }

        /// <summary>
        /// The method is called in a separate thread when the connection is established.
        /// </summary>
        /// <param name="asyncResult"></param>
        protected override void HandleConnection(IAsyncResult asyncResult)
        {
            using (EneterTrace.Entering())
            {
                TcpListener aListener = (TcpListener)asyncResult.AsyncState;

                try
                {
                    TcpClient aTcpClient = aListener.EndAcceptTcpClient(asyncResult);

                    lock (myConnectedSenders)
                    {
                        myConnectedSenders.Add(aTcpClient);
                    }

                    try
                    {
                        // If the end is requested.
                        if (!myStopTcpListeningRequested)
                        {
                            // Source stream.
                            Stream anInputStream = null;

                            if (mySecurityStreamFactory != null)
                            {
                                anInputStream = mySecurityStreamFactory.CreateSecurityStreamAndAuthenticate(aTcpClient.GetStream());
                            }
                            else
                            {
                                anInputStream = aTcpClient.GetStream();
                            }


                            // First read the message to the buffer.
                            object aMessage = null;
                            using (MemoryStream aMemStream = new MemoryStream())
                            {
                                int aSize = 0;
                                byte[] aBuffer = new byte[32768];
                                while ((aSize = anInputStream.Read(aBuffer, 0, aBuffer.Length)) != 0)
                                {
                                    aMemStream.Write(aBuffer, 0, aSize);
                                }

                                // Read the message from the buffer.
                                aMemStream.Position = 0;
                                aMessage = MessageStreamer.ReadMessage(aMemStream);
                            }


                            if (!myStopTcpListeningRequested && aMessage != null)
                            {
                                // Put the message to the queue from where the working thread removes it to notify
                                // subscribers of the input channel.
                                // Note: therfore subscribers of the input channel are notified allways in one thread.
                                myMessageProcessingThread.EnqueueMessage(aMessage);
                            }
                        }
                    }
                    finally
                    {
                        lock (myConnectedSenders)
                        {
                            myConnectedSenders.Remove(aTcpClient);
                        }

                        // Close the connection.
                        aTcpClient.Close();
                    }
                }
                catch (ObjectDisposedException)
                {
                    // This sometime happens when the listening was closed.
                    // The exception can be ignored because during closing the messages are not expected.
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.ProcessingHttpConnectionFailure, err);
                }
            }
        }

        /// <summary>
        /// The method is called from the working thread when a message shall be processed.
        /// Messages comming from from diffrent receiving threads are put to the queue where the working
        /// thread removes them one by one and notify the subscribers on the input channel.
        /// Therefore the channel notifies always in one thread.
        /// </summary>
        protected override void MessageHandler(object message)
        {
            using (EneterTrace.Entering())
            {
                if (MessageReceived != null)
                {
                    try
                    {
                        MessageReceived(this, new ChannelMessageEventArgs(ChannelId, message));
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

        private List<TcpClient> myConnectedSenders = new List<TcpClient>();

        protected override string TracedObject
        {
            get 
            {
                return "Tcp input channel '" + ChannelId + "' "; 
            }
        }
    }
}

#endif