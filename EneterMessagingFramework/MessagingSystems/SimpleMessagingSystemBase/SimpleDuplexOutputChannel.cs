/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.DataProcessing.Streaming;
using System.Threading;

namespace Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase
{
    internal class SimpleDuplexOutputChannel : IDuplexOutputChannel
    {
        public event EventHandler<DuplexChannelEventArgs> ConnectionOpened;

        public event EventHandler<DuplexChannelEventArgs> ConnectionClosed;

        public event EventHandler<DuplexChannelMessageEventArgs> ResponseMessageReceived;

        public SimpleDuplexOutputChannel(string channelId, string responseReceiverId, IMessagingSystemFactory messagingFactory)
            : this(channelId, responseReceiverId, messagingFactory, null)
        {
        }

        public SimpleDuplexOutputChannel(string channelId, string responseReceiverId, IMessagingSystemFactory messagingFactory, ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                if (string.IsNullOrEmpty(channelId))
                {
                    EneterTrace.Error(ErrorHandler.NullOrEmptyChannelId);
                    throw new ArgumentException(ErrorHandler.NullOrEmptyChannelId);
                }

                myMessagingFactory = messagingFactory;

                ResponseReceiverId = (string.IsNullOrEmpty(responseReceiverId)) ? channelId + "_" + Guid.NewGuid().ToString() : responseReceiverId;

                // Try to create input channel to check, if the response receiver id is correct.
                myMessagingFactory.CreateInputChannel(ResponseReceiverId);

                myMessageSenderOutputChannel = myMessagingFactory.CreateOutputChannel(channelId);
                mySerializer = serializer;
            }
        }

        public string ChannelId { get { return myMessageSenderOutputChannel.ChannelId; } }

        public string ResponseReceiverId { get; private set; }

        public void OpenConnection()
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    if (IsConnected)
                    {
                        string aMessage = TracedObject + ErrorHandler.IsAlreadyConnected;
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    try
                    {
                        // Create the input channel listening to responses.
                        myResponseReceiverInputChannel = myMessagingFactory.CreateInputChannel(ResponseReceiverId);
                        myResponseReceiverInputChannel.MessageReceived += OnResponseMessageReceived;
                        myResponseReceiverInputChannel.StartListening();

                        // Send open connection message with receiver id.
                        object[] anOpenConnectionMessage = MessageStreamer.GetOpenConnectionMessage(ResponseReceiverId);
                        if (mySerializer == null)
                        {
                            myMessageSenderOutputChannel.SendMessage(anOpenConnectionMessage);
                        }
                        else
                        {
                            object aSerializedMessage = mySerializer.Serialize<object[]>(anOpenConnectionMessage);
                            myMessageSenderOutputChannel.SendMessage(aSerializedMessage);
                        }

                        // Invoke the event notifying, the connection was opened.
                        NotifyConnectionOpened();
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.OpenConnectionFailure, err);

                        try
                        {
                            CloseConnection();
                        }
                        catch
                        {
                            // We tried to clean after failure. The exception can be ignored.
                        }

                        throw;
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
                    // Try to notify that the connection is closed
                    if (myMessageSenderOutputChannel != null && !string.IsNullOrEmpty(ResponseReceiverId))
                    {
                        try
                        {
                            object[] aCloseConnectionMessage = MessageStreamer.GetCloseConnectionMessage(ResponseReceiverId);
                            if (mySerializer == null)
                            {
                                myMessageSenderOutputChannel.SendMessage(aCloseConnectionMessage);
                            }
                            else
                            {
                                object aSerializedMessage = mySerializer.Serialize<object[]>(aCloseConnectionMessage);
                                myMessageSenderOutputChannel.SendMessage(aSerializedMessage);
                            }
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.CloseConnectionFailure, err);
                        }
                    }

                    StopListening();
                }

                NotifyConnectionClosed();
            }
        }

        public bool IsConnected
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    lock (myConnectionManipulatorLock)
                    {
                        return (myResponseReceiverInputChannel != null);
                    }
                }
            }
        }

        public void SendMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    if (!IsConnected)
                    {
                        string aMessage = TracedObject + ErrorHandler.SendMessageNotConnectedFailure;
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    try
                    {
                        object[] aMessage = { (byte)2, myResponseReceiverInputChannel.ChannelId, message };
                        if (mySerializer == null)
                        {
                            myMessageSenderOutputChannel.SendMessage(aMessage);
                        }
                        else
                        {
                            object aSerializedMessage = mySerializer.Serialize<object[]>(aMessage);
                            myMessageSenderOutputChannel.SendMessage(aSerializedMessage);
                        }
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.SendMessageFailure, err);
                        throw;
                    }
                }
            }
        }

        private void OnResponseMessageReceived(object sender, ChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                object aMessage = null;
                if (mySerializer == null)
                {
                    aMessage = e.Message;
                }
                else
                {
                    object[] aDeserializedMessage = mySerializer.Deserialize<object[]>(e.Message);
                    
                    // Note: If the serializer is defined, then SimpleDuplexInputChannel during sending of
                    //       response messages (response message, connecion close message) serializes them
                    //       to object[].
                    //       If the object[] length is 1 then it is just a normal response message.
                    //       Otherwise it can be the close connection message.
                    aMessage = (aDeserializedMessage.Length == 1) ? aDeserializedMessage[0] : aDeserializedMessage;
                }

                // If the message indicates the disconnection.
                if (MessageStreamer.IsCloseConnectionMessage(aMessage))
                {
                    // Stop listening to the input channel for the response message.
                    // Note: The duplex input channel notifies that this duplex output channel is dosconnected.
                    //       Therefore, it is not needed, this channel will send the message closing the connection.
                    //       Therefore, it is enough just to stop listening to response messages.
                    StopListening();

                    NotifyConnectionClosed();
                }
                // It is the normal response message - notify the subscribed handler.
                else if (ResponseMessageReceived != null)
                {
                    try
                    {
                        ResponseMessageReceived(this, new DuplexChannelMessageEventArgs(ChannelId, aMessage, ResponseReceiverId));
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

        private void StopListening()
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    if (myResponseReceiverInputChannel != null)
                    {
                        // Stop the listener
                        try
                        {
                            myResponseReceiverInputChannel.StopListening();
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.StopListeningFailure, err);
                        }
                        finally
                        {
                            myResponseReceiverInputChannel.MessageReceived -= OnResponseMessageReceived;
                            myResponseReceiverInputChannel = null;
                        }
                    }
                }
            }
        }

        private void NotifyConnectionOpened()
        {
            using (EneterTrace.Entering())
            {
                WaitCallback aConnectionOpenedInvoker = x =>
                    {
                        using (EneterTrace.Entering())
                        {
                            try
                            {
                                if (ConnectionOpened != null)
                                {
                                    DuplexChannelEventArgs aMsg = new DuplexChannelEventArgs(ChannelId, ResponseReceiverId);
                                    ConnectionOpened(this, aMsg);
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

        private void NotifyConnectionClosed()
        {
            using (EneterTrace.Entering())
            {
                WaitCallback aConnectionClosedInvoker = x =>
                {
                    using (EneterTrace.Entering())
                    {
                        try
                        {
                            if (ConnectionClosed != null)
                            {
                                DuplexChannelEventArgs aMsg = new DuplexChannelEventArgs(ChannelId, ResponseReceiverId);
                                ConnectionClosed(this, aMsg);
                            }
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                        }
                    }
                };

                // Invoke the event in a different thread.
                ThreadPool.QueueUserWorkItem(aConnectionClosedInvoker);
            }
        }


        private IMessagingSystemFactory myMessagingFactory;

        private IInputChannel myResponseReceiverInputChannel;
        private IOutputChannel myMessageSenderOutputChannel;

        private object myConnectionManipulatorLock = new object();

        // If the serializer is set then it is used to serialize messages.
        // Note: Duplex channels do not send messages as they are but they structure them.
        //       The problem is that Silverlight must have string messages.
        private ISerializer mySerializer;

        private string TracedObject
        {
            get
            {
                return "The duplex output channel '" + ChannelId + "' ";
            }
        }
    }
}
