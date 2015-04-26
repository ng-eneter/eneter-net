/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/


#if !SILVERLIGHT || WINDOWS_PHONE80 || WINDOWS_PHONE81

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Eneter.Messaging.DataProcessing.MessageQueueing;
using Eneter.Messaging.DataProcessing.Streaming;
using Eneter.Messaging.Diagnostic;

#if WINDOWS_PHONE80 || WINDOWS_PHONE81
using Eneter.Messaging.MessagingSystems.TcpMessagingSystem;
using System.Collections.ObjectModel;
#endif

namespace Eneter.Messaging.MessagingSystems.WebSocketMessagingSystem
{
    internal class WebSocketClientContext : IWebSocketClientContext
    {
        private enum EMessageInSendProgress
        {
            None,
            Binary,
            Text
        }

        public event EventHandler ConnectionClosed;

        public event EventHandler PongReceived;

        public WebSocketClientContext(Uri address, IDictionary<string, string> headerFields, TcpClient tcpClient, Stream dataStream)
        {
            using (EneterTrace.Entering())
            {
                Uri = address;
                HeaderFields = new ReadOnlyDictionary<string, string>(headerFields);
                myTcpClient = tcpClient;
                myClientStream = dataStream;
                ClientEndPoint = tcpClient.Client.RemoteEndPoint as IPEndPoint;

                // Infinite time for sending and receiving messages.
                // Timeouts can be reconfigured when the connection is handled.
                SendTimeout = 0;
                ReceiveTimeout = 0;
            }
        }

        public Uri Uri { get; private set; }

        public IDictionary<string, string> HeaderFields { get; private set; }

        public IPEndPoint ClientEndPoint { get; private set; }

        public int SendTimeout
        {
            get { return myTcpClient.SendTimeout; }
            set { myTcpClient.SendTimeout = value; }
        }

        public int ReceiveTimeout
        {
            get { return myTcpClient.ReceiveTimeout; }
            set { myTcpClient.ReceiveTimeout = value; }
        }

        public bool IsConnected
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    lock (myConnectionManipulatorLock)
                    {
                        return myTcpClient != null && myIsListeningToResponses;
                    }
                }
            }
        }

        public void CloseConnection()
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    myStopReceivingRequestedFlag = true;
                    myMessageInSendProgress = EMessageInSendProgress.None;

                    if (myTcpClient != null)
                    {
                        // Try to send the frame closing the communication.
                        if (myClientStream != null)
                        {
                            try
                            {
                                // If it was not disconnected by the client then try to send the message the connection was closed.
                                if (myIsListeningToResponses)
                                {
                                    // Generate the masking key.
                                    byte[] aCloseFrame = WebSocketFormatter.EncodeCloseFrame(null, 1000);
                                    myClientStream.Write(aCloseFrame, 0, aCloseFrame.Length);
                                }
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.FailedToCloseConnection, err);
                            }

                            myClientStream.Close();
                        }

                        try
                        {
                            myTcpClient.Close();
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + "failed to close Tcp connection.", err);
                        }

                        myTcpClient = null;
                    }

                    myReceivedMessages.UnblockProcessingThreads();
                }
            }
        }

        public void SendMessage(object data)
        {
            using (EneterTrace.Entering())
            {
                SendMessage(data, true);
            }
        }

        public void SendMessage(object data, bool isFinal)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    // If there is no message that was not finalized yet then send the binary or text data frame.
                    if (myMessageInSendProgress == EMessageInSendProgress.None)
                    {
                        if (data is byte[])
                        {
                            SendFrame(maskingKey => WebSocketFormatter.EncodeBinaryMessageFrame(isFinal, maskingKey, (byte[])data));

                            if (isFinal == false)
                            {
                                myMessageInSendProgress = EMessageInSendProgress.Binary;
                            }
                        }
                        else if (data is string)
                        {
                            SendFrame(maskingKey => WebSocketFormatter.EncodeTextMessageFrame(isFinal, maskingKey, (string)data));

                            if (isFinal == false)
                            {
                                myMessageInSendProgress = EMessageInSendProgress.Text;
                            }
                        }
                        else
                        {
                            string anErrorMessage = TracedObject + "failed to send the message because input parameter data is not byte[] or string.";
                            EneterTrace.Error(anErrorMessage);
                            throw new ArgumentException(anErrorMessage);
                        }
                    }
                    // If there is a binary message that was sent only partialy - was not finalized yet.
                    else if (myMessageInSendProgress == EMessageInSendProgress.Binary)
                    {
                        if (data is byte[])
                        {
                            SendFrame(maskingKey => WebSocketFormatter.EncodeContinuationMessageFrame(isFinal, maskingKey, (byte[])data));

                            if (isFinal == true)
                            {
                                myMessageInSendProgress = EMessageInSendProgress.None;
                            }
                        }
                        else
                        {
                            string anErrorMessage = TracedObject + "failed to send the continuation binary message because input parameter data was not byte[].";
                            EneterTrace.Error(anErrorMessage);
                            throw new ArgumentException(anErrorMessage);
                        }
                    }
                    // If there is a text message that was sent only partialy - was not finalized yet.
                    else
                    {
                        if (data is string)
                        {
                            SendFrame(maskingKey => WebSocketFormatter.EncodeContinuationMessageFrame(isFinal, maskingKey, (string)data));

                            if (isFinal == true)
                            {
                                myMessageInSendProgress = EMessageInSendProgress.None;
                            }
                        }
                        else
                        {
                            string anErrorMessage = TracedObject + "failed to send the continuation text message because input parameter data was not string.";
                            EneterTrace.Error(anErrorMessage);
                            throw new ArgumentException(anErrorMessage);
                        }
                    }
                }
            }
        }

        public void SendPing()
        {
            using (EneterTrace.Entering())
            {
                SendFrame(maskingKey => WebSocketFormatter.EncodePingFrame(maskingKey));
            }
        }

        public void SendPong()
        {
            using (EneterTrace.Entering())
            {
                SendFrame(maskingKey => WebSocketFormatter.EncodePongFrame(maskingKey, null));
            }
        }

        public WebSocketMessage ReceiveMessage()
        {
            using (EneterTrace.Entering())
            {
                return myReceivedMessages.DequeueMessage();
            }
        }

        private void SendFrame(Func<byte[], byte[]> formatter)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    if (!IsConnected)
                    {
                        string aMessage = TracedObject + ErrorHandler.FailedToSendMessageBecauseNotConnected;
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    try
                    {
                        // Encode the message frame.
                        // Note: According to the protocol, server shall not mask sent data.
                        byte[] aFrame = formatter(null);

                        // Send the message.
                        myClientStream.Write(aFrame, 0, aFrame.Length);
                        //myClientStream.Flush();
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.FailedToSendMessage, err);
                        throw;
                    }
                }
            }
        }

        public void DoRequestListening()
        {
            using (EneterTrace.Entering())
            {
                myIsListeningToResponses = true;
                ushort aCloseCode = 0;

                try
                {
                    DynamicStream aContinuousMessageStream = null;

                    while (!myStopReceivingRequestedFlag)
                    {
                        // Decode the incoming message.
                        WebSocketFrame aFrame = WebSocketFormatter.DecodeFrame(myClientStream);

                        if (!myStopReceivingRequestedFlag && aFrame != null)
                        {
                            // Frames from server must be unmasked.
                            // According the protocol, If the frame was NOT masked, the server must close connection with the client.
                            if (aFrame.MaskFlag == false)
                            {
                                throw new InvalidOperationException(TracedObject + "received unmasked frame from the client. Frames from client shall be masked.");
                            }

                            // Process the frame.
                            if (aFrame.FrameType == EFrameType.Ping)
                            {
                                // Response 'pong'. The response responses same data as received in the 'ping'.
                                SendFrame(maskingKey => WebSocketFormatter.EncodePongFrame(maskingKey, aFrame.Message));
                            }
                            else if (aFrame.FrameType == EFrameType.Close)
                            {
                                EneterTrace.Debug(TracedObject + "received the close frame.");
                                break;
                            }
                            else if (aFrame.FrameType == EFrameType.Pong)
                            {
                                Notify(PongReceived);
                            }
                            // If a new message starts.
                            else if (aFrame.FrameType == EFrameType.Binary || aFrame.FrameType == EFrameType.Text)
                            {
                                // If a previous message is not finished then the new message is not expected -> protocol error.
                                if (aContinuousMessageStream != null)
                                {
                                    EneterTrace.Warning(TracedObject + "detected unexpected new message. (previous message was not finished)");

                                    // Protocol error close code.
                                    aCloseCode = 1002;
                                    break;
                                }

                                WebSocketMessage aReceivedMessage = null;

                                // If the message does not come in multiple frames then optimize the performance
                                // and use MemoryStream instead of DynamicStream.
                                if (aFrame.IsFinal)
                                {
                                    MemoryStream aMessageStream = new MemoryStream(aFrame.Message);
                                    aReceivedMessage = new WebSocketMessage(aFrame.FrameType == EFrameType.Text, aMessageStream);
                                }
                                else
                                // if the message is split to several frames then use DynamicStream so that writing of incoming
                                // frames and reading of already received data can run in parallel.
                                {
                                    // Create stream where the message data will be writen.
                                    aContinuousMessageStream = new DynamicStream();
                                    aContinuousMessageStream.WriteWithoutCopying(aFrame.Message, 0, aFrame.Message.Length);
                                    aReceivedMessage = new WebSocketMessage(aFrame.FrameType == EFrameType.Text, aContinuousMessageStream);
                                }

                                // Put received message to the queue.
                                myReceivedMessages.EnqueueMessage(aReceivedMessage);
                            }
                            // If a message continues. (I.e. message is split into more fragments.)
                            else if (aFrame.FrameType == EFrameType.Continuation)
                            {
                                // If the message does not exist then continuing frame does not have any sense -> protocol error.
                                if (aContinuousMessageStream == null)
                                {
                                    EneterTrace.Warning(TracedObject + "detected unexpected continuing of a message. (none message was started before)");

                                    // Protocol error close code.
                                    aCloseCode = 1002;
                                    break;
                                }

                                aContinuousMessageStream.WriteWithoutCopying(aFrame.Message, 0, aFrame.Message.Length);

                                // If this is the final frame.
                                if (aFrame.IsFinal)
                                {
                                    aContinuousMessageStream.IsBlockingMode = false;
                                    aContinuousMessageStream = null;
                                }
                            }
                        }

                        // If disconnected
                        if (aFrame == null)// || !myTcpClient.Client.Poll(0, SelectMode.SelectWrite))
                        {
                            //EneterTrace.Warning(TracedObject + "detected the TCP connection is not available. The connection will be closed.");
                            break;
                        }
                    }
                }
                catch (IOException)
                {
                    // Ignore this exception. It is often thrown when the connection was closed.
                    // Do not thrace this because the tracing degradates the performance in this case.
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.FailedInListeningLoop, err);
                }

                // If the connection is being closed due to a protocol error.
                if (aCloseCode > 1000)
                {
                    // Try to send the close message.
                    try
                    {
                        byte[] aCloseMessage = WebSocketFormatter.EncodeCloseFrame(null, aCloseCode);
                        myClientStream.Write(aCloseMessage, 0, aCloseMessage.Length);
                    }
                    catch
                    {
                    }
                }

                myIsListeningToResponses = false;

                myReceivedMessages.UnblockProcessingThreads();

                // Notify the listening to messages stoped.
                Notify(ConnectionClosed);
            }
        }


        private void Notify(EventHandler eventHandler)
        {
            using (EneterTrace.Entering())
            {
                WaitCallback aConnectionOpenedInvoker = x =>
                {
                    using (EneterTrace.Entering())
                    {
                        try
                        {
                            if (eventHandler != null)
                            {
                                eventHandler(this, new EventArgs());
                            }
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                        }
                    }
                };

                // Invoke the event in a different thread.
                ThreadPool.QueueUserWorkItem(aConnectionOpenedInvoker);
            }
        }

        private TcpClient myTcpClient;
        private Stream myClientStream;

        private object myConnectionManipulatorLock = new object();

        private bool myStopReceivingRequestedFlag;
        private bool myIsListeningToResponses;

        private EMessageInSendProgress myMessageInSendProgress = EMessageInSendProgress.None;

        private MessageQueue<WebSocketMessage> myReceivedMessages = new MessageQueue<WebSocketMessage>();
        

        private string TracedObject
        {
            get
            {
                string anAddress = (Uri != null) ? Uri.ToString() : "";
                return GetType().Name + " '" + anAddress + "' ";
            }
        }
    }
}


#endif