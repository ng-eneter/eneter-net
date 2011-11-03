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

namespace Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase
{
    internal class SimpleDuplexInputChannel : IDuplexInputChannel
    {
        public event EventHandler<DuplexChannelMessageEventArgs> MessageReceived;

        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;

        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;

        public SimpleDuplexInputChannel(string channelId, IMessagingSystemFactory messagingFactory)
            : this(channelId, messagingFactory, null)
        {
        }

        public SimpleDuplexInputChannel(string channelId, IMessagingSystemFactory messagingFactory, ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                if (string.IsNullOrEmpty(channelId))
                {
                    EneterTrace.Error(ErrorHandler.NullOrEmptyChannelId);
                    throw new ArgumentException(ErrorHandler.NullOrEmptyChannelId);
                }

                myDuplexInputChannelId = channelId;
                myMessagingSystemFactory = messagingFactory;
                mySerializer = serializer;
            }
        }

        public string ChannelId { get { return myDuplexInputChannelId; } }

        public void StartListening()
        {
            using (EneterTrace.Entering())
            {
                lock (myListeningManipulatorLock)
                {
                    if (IsListening)
                    {
                        string aMessage = TracedObject + ErrorHandler.IsAlreadyListening;
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    try
                    {
                        myMessageReceiverInputChannel = myMessagingSystemFactory.CreateInputChannel(myDuplexInputChannelId);
                        myMessageReceiverInputChannel.MessageReceived += OnMessageReceived;
                        myMessageReceiverInputChannel.StartListening();
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.StartListeningFailure, err);

                        try
                        {
                            StopListening();
                        }
                        catch (Exception)
                        {
                            // We tried to clean after the failure. We can ignore this exception.
                        }

                        throw;
                    }
                }
            }
        }


        public void StopListening()
        {
            using (EneterTrace.Entering())
            {
                lock (myListeningManipulatorLock)
                {
                    // Stop listening of the 
                    if (myMessageReceiverInputChannel != null)
                    {
                        try
                        {
                            myMessageReceiverInputChannel.StopListening();
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.StopListeningFailure, err);
                        }

                        myMessageReceiverInputChannel.MessageReceived -= OnMessageReceived;
                    }
                }
            }
        }

        public bool IsListening
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    lock (myListeningManipulatorLock)
                    {
                        return myMessageReceiverInputChannel != null && myMessageReceiverInputChannel.IsListening;
                    }
                }
            }
        }

        public void SendResponseMessage(string responseReceiverId, object message)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    IOutputChannel aResponseOutputChannel = myMessagingSystemFactory.CreateOutputChannel(responseReceiverId);

                    if (mySerializer == null)
                    {
                        aResponseOutputChannel.SendMessage(message);
                    }
                    else
                    {
                        object[] aMessageToSend = { message };
                        object aSerializedResponseMessage = mySerializer.Serialize<object[]>(aMessageToSend);
                        aResponseOutputChannel.SendMessage(aSerializedResponseMessage);
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.SendResponseFailure, err);

                    // Sending the response message failed, therefore consider it as the disconnection with the reponse receiver.
                    NotifyResponseReceiverDisconnected(responseReceiverId);

                    throw;
                }
            }
        }

        public void DisconnectResponseReceiver(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    IOutputChannel aResponseOutputChannel = myMessagingSystemFactory.CreateOutputChannel(responseReceiverId);

                    // Notify the response receiver about the disconnection.
                    object[] aCloseConnectionMessage = MessageStreamer.GetCloseConnectionMessage(responseReceiverId);

                    if (mySerializer == null)
                    {
                        aResponseOutputChannel.SendMessage(aCloseConnectionMessage);
                    }
                    else
                    {
                        object aSerializedCloseConnectionMessage = mySerializer.Serialize<object[]>(aCloseConnectionMessage);
                        aResponseOutputChannel.SendMessage(aSerializedCloseConnectionMessage);
                    }
                    
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.DisconnectResponseReceiverFailure + responseReceiverId, err);
                }
            }
        }


        private void OnMessageReceived(object sender, ChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    object[] aMessage = null;
                    if (mySerializer == null)
                    {
                        aMessage = (object[])e.Message;
                    }
                    else
                    {
                        aMessage = mySerializer.Deserialize<object[]>(e.Message);
                    }

                    if (MessageStreamer.IsOpenConnectionMessage(aMessage))
                    {
                        NotifyResponseReceiverConnected((string)aMessage[1]);
                    }
                    else if (MessageStreamer.IsCloseConnectionMessage(aMessage))
                    {
                        NotifyResponseReceiverDisconnected((string)aMessage[1]);
                    }
                    else if (MessageStreamer.IsRequestMessage(aMessage))
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
            using (EneterTrace.Entering())
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


        private IMessagingSystemFactory myMessagingSystemFactory;
        private IInputChannel myMessageReceiverInputChannel;

        /// <summary>
        /// Serializer can be defined if a certain data type is required for the messaging.
        /// E.g. Silverlight messaging can transfer only strings.
        /// </summary>
        private ISerializer mySerializer;

        private object myListeningManipulatorLock = new object();

        private string myDuplexInputChannelId = "";
        private string TracedObject
        {
            get
            {
                return "The duplex input channel '" + myDuplexInputChannelId + "' ";
            }
        }
    }
}
