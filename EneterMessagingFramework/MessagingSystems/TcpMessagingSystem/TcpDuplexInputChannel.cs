/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/


#if !SILVERLIGHT
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using Eneter.Messaging.DataProcessing.Streaming;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security;

namespace Eneter.Messaging.MessagingSystems.TcpMessagingSystem
{
    internal class TcpDuplexInputChannel : TcpInputChannelBase, IDuplexInputChannel
    {
        /// <summary>
        /// The internal class representing the connection with the client.
        /// </summary>
        private class TClient
        {
            public enum EConnectionState
            {
                Open,
                Closed
            }

            public TClient(TcpClient tcpClient, Stream communicationStream)
            {
                TcpClient = tcpClient;
                CommunicationStream = communicationStream;
                ConnectionState = EConnectionState.Open;
            }

            public EConnectionState ConnectionState { get; set; }

            /// <summary>
            /// Returns Tcp client.
            /// </summary>
            public TcpClient TcpClient { get; private set; }

            /// <summary>
            /// Returns the stream that shall be used to receive messages and send response messages.
            /// Note: If the communication is secured, then it returns the secure stream.
            ///       If the communication is not secured, then it returns TcpClient.GetStream().
            /// </summary>
            public Stream CommunicationStream { get; private set; }
        }

        public event EventHandler<DuplexChannelMessageEventArgs> MessageReceived;

        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;

        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;


        public TcpDuplexInputChannel(string ipAddressAndPort, ISecurityFactory securityStreamFactory)
            : base(ipAddressAndPort, securityStreamFactory)
        {
            using (EneterTrace.Entering())
            {
            }
        }


        /// <summary>
        /// Sends the response message back to the TcpDuplexOutputChannel.
        /// </summary>
        /// <param name="responseReceiverId"></param>
        /// <param name="message"></param>
        public void SendResponseMessage(string responseReceiverId, object message)
        {
            using (EneterTrace.Entering())
            {
                TClient aClient;
                lock (myConnectedResponseReceivers)
                {
                    myConnectedResponseReceivers.TryGetValue(responseReceiverId, out aClient);
                }

                if (aClient != null)
                {
                    try
                    {
                        using (MemoryStream aMemoryStream = new MemoryStream())
                        {
                            MessageStreamer.WriteMessage(aMemoryStream, message);
                            aMemoryStream.WriteTo(aClient.CommunicationStream);
                        }
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.SendResponseFailure, err);

                        aClient.CommunicationStream.Close();

                        try
                        {
                            aClient.TcpClient.Close();
                        }
                        catch
                        {
                            // do not care if an exception during closing the tcp client.
                        }

                        lock (myConnectedResponseReceivers)
                        {
                            myConnectedResponseReceivers.Remove(responseReceiverId);
                        }

                        // Put the message about the disconnections to the queue from where the working thread removes it to notify
                        // subscribers of the input channel.
                        // Note: therfore subscribers of the input channel are notified allways in one thread.
                        object aCloseConnectionMessage = MessageStreamer.GetCloseConnectionMessage(responseReceiverId);
                        myMessageProcessingThread.EnqueueMessage(aCloseConnectionMessage);

                        throw;
                    }
                }
                else
                {
                    string anError = TracedObject + ErrorHandler.SendMessageNotConnectedFailure;
                    EneterTrace.Error(anError);
                    throw new InvalidOperationException(anError);
                }
            }
        }

        public void DisconnectResponseReceiver(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectedResponseReceivers)
                {
                    TClient aClient;
                    myConnectedResponseReceivers.TryGetValue(responseReceiverId, out aClient);
                    if (aClient != null)
                    {
                        aClient.CommunicationStream.Close();
                        aClient.TcpClient.Close();
                        aClient.ConnectionState = TClient.EConnectionState.Closed;
                    }
                }
            }
        }

        protected override void DisconnectClients()
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectedResponseReceivers)
                {
                    foreach (KeyValuePair<string, TClient> aConnection in myConnectedResponseReceivers)
                    {
                        aConnection.Value.CommunicationStream.Close();
                        aConnection.Value.TcpClient.Close();
                    }
                    myConnectedResponseReceivers.Clear();
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
                    string aResponseReceiverId = ""; // will be set when the 1st message is received.

                    Stream anInputOutputStream = null;

                    try
                    {
                        // If the end is requested.
                        if (!myStopTcpListeningRequested)
                        {
                            // Get the stream to receive messages and send response messages.
                            if (mySecurityStreamFactory != null)
                            {
                                // If the security communication is required, then wrap the network stream into the security stream.
                                anInputOutputStream = mySecurityStreamFactory.CreateSecurityStreamAndAuthenticate(aTcpClient.GetStream());
                            }
                            else
                            {
                                // Non-secure communication.
                                anInputOutputStream = aTcpClient.GetStream();
                            }

                            // While the stop of listening is not requested and the connection is not closed.
                            bool isConnectionClosed = false;
                            while (!myStopTcpListeningRequested && !isConnectionClosed)
                            {
                                // Block until a message is received or the connection is closed.
                                object aMessage = MessageStreamer.ReadMessage(anInputOutputStream);

                                if (!myStopTcpListeningRequested)
                                {
                                    if (aMessage != null)
                                    {
                                        object[] aStructuredMessage = (object[])aMessage;

                                        // If response receiver connection open message
                                        if (MessageStreamer.IsOpenConnectionMessage(aMessage))
                                        {
                                            aResponseReceiverId = (string)aStructuredMessage[1];

                                            lock (myConnectedResponseReceivers)
                                            {
                                                // Note: It is not allowed that 2 response receivers would have the same responseReceiverId.
                                                if (!myConnectedResponseReceivers.ContainsKey(aResponseReceiverId))
                                                {
                                                    myConnectedResponseReceivers.Add(aResponseReceiverId, new TClient(aTcpClient, anInputOutputStream));
                                                }
                                                else
                                                {
                                                    throw new InvalidOperationException("The resposne receiver '" + aResponseReceiverId + "' is already connected. It is not allowed, that response receivers share the same id.");
                                                }
                                            }

                                            // Put the message to the queue from where the working thread removes it to notify
                                            // subscribers of the input channel.
                                            // Note: therfore subscribers of the input channel are notified allways in one thread.
                                            myMessageProcessingThread.EnqueueMessage(aMessage);
                                        }
                                        // If response receiver connection closed message
                                        else if (MessageStreamer.IsCloseConnectionMessage(aMessage))
                                        {
                                            isConnectionClosed = true;
                                        }
                                        else
                                        {
                                            // Put the message to the queue from where the working thread removes it to notify
                                            // subscribers of the input channel.
                                            // Note: therfore subscribers of the input channel are notified allways in one thread.
                                            myMessageProcessingThread.EnqueueMessage(aMessage);
                                        }
                                    }
                                    else
                                    {
                                        isConnectionClosed = true;
                                    }
                                }
                            }
                        }
                    }
                    finally
                    {
                        if (!string.IsNullOrEmpty(aResponseReceiverId))
                        {
                            TClient.EConnectionState aConnectionState = TClient.EConnectionState.Closed;

                            lock (myConnectedResponseReceivers)
                            {
                                TClient aTClient;
                                myConnectedResponseReceivers.TryGetValue(aResponseReceiverId, out aTClient);
                                if (aTClient != null)
                                {
                                    aConnectionState = aTClient.ConnectionState;
                                }

                                myConnectedResponseReceivers.Remove(aResponseReceiverId);
                            }

                            // If the connection was not closed from this duplex input channel (i.e. by stopping of listener
                            // or by calling 'DisconnectResponseReceiver()', then notify, that the client disconnected itself.
                            if (!myStopTcpListeningRequested && aConnectionState == TClient.EConnectionState.Open)
                            {
                                object aCloseConnectionMessage = MessageStreamer.GetCloseConnectionMessage(aResponseReceiverId);

                                // Put the message to the queue from where the working thread removes it to notify
                                // subscribers of the input channel.
                                // Note: therfore subscribers of the input channel are notified allways in one thread.
                                myMessageProcessingThread.EnqueueMessage(aCloseConnectionMessage);
                            }
                        }

                        // Close the connection.
                        if (anInputOutputStream != null)
                        {
                            anInputOutputStream.Close();
                        }
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
                    EneterTrace.Error(TracedObject + ErrorHandler.ProcessingTcpConnectionFailure, err);
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
                try
                {
                    object[] aMessage = (object[])message;

                    if (MessageStreamer.IsOpenConnectionMessage(message))
                    {
                        NotifyResponseReceiverConnected((string)aMessage[1]);
                    }
                    else if (MessageStreamer.IsCloseConnectionMessage(message))
                    {
                        NotifyResponseReceiverDisconnected((string)aMessage[1]);
                    }
                    else if (MessageStreamer.IsRequestMessage(message))
                    {
                        NotifyMessageReceived(ChannelId, aMessage[2], (string)aMessage[1]);
                    }
                    else
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.ReceiveMessageIncorrectFormatFailure);
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.ReceiveMessageFailure, err);
                }
            }
        }


        private void NotifyResponseReceiverConnected(string responseReceiverId)
        {
            if (ResponseReceiverConnected != null)
            {
                ResponseReceiverEventArgs aResponseReceiverEvent = new ResponseReceiverEventArgs(responseReceiverId);

                try
                {
                    ResponseReceiverConnected(this, aResponseReceiverEvent);
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                }
            }
        }

        private void NotifyResponseReceiverDisconnected(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                if (ResponseReceiverDisconnected != null)
                {
                    ResponseReceiverEventArgs aResponseReceiverEvent = new ResponseReceiverEventArgs(responseReceiverId);

                    try
                    {
                        ResponseReceiverDisconnected(this, aResponseReceiverEvent);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }

        private void NotifyMessageReceived(string channelId, object message, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                if (MessageReceived != null)
                {
                    try
                    {
                        MessageReceived(this, new DuplexChannelMessageEventArgs(channelId, message, responseReceiverId));
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

        private Dictionary<string, TClient> myConnectedResponseReceivers = new Dictionary<string, TClient>();


        protected override string TracedObject
        {
            get 
            {
                return "Tcp duplex input channel '" + ChannelId + "' "; 
            }
        }
    }
}

#endif
