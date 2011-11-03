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
using System.Linq;
using System.Timers;
using Eneter.Messaging.DataProcessing.Streaming;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.Nodes.Bridge
{
    internal class DuplexBridge : IDuplexBridge
    {
        private class TConnection
        {
            public TConnection(IDuplexOutputChannel duplexOutputChannel, string responseReceiverId)
            {
                DuplexOutputChannel = duplexOutputChannel;
                ResponseReceiverId = responseReceiverId;
                LastActivityTime = DateTime.Now;
                Messages = new Queue<object>();
            }

            public IDuplexOutputChannel DuplexOutputChannel { get; private set; }
            public string ResponseReceiverId { get; private set; }
            public DateTime LastActivityTime { get; set; }
            public Queue<object> Messages { get; private set; }
        }


        public DuplexBridge(string channelId, IMessagingSystemFactory outputMessagingSystemFactory, int requesterInactivityTimeout, int maximumSizeOfOneResponse)
        {
            using (EneterTrace.Entering())
            {
                if (string.IsNullOrEmpty(channelId))
                {
                    EneterTrace.Error(ErrorHandler.NullOrEmptyChannelId);
                    throw new ArgumentException(ErrorHandler.NullOrEmptyChannelId);
                }

                myChannelId = channelId;
                myOutputMessagingFactory = outputMessagingSystemFactory;
                myMaximumSizeOfOneResponse = maximumSizeOfOneResponse;

                // Initialize the timer to regularly check the timeout for connections with duplex output channels.
                // If the duplex output channel did not pull within the timeout then the connection
                // is closed and the removed from the list.
                // Note: The timer is set here but not executed.
                myResponseReceiverInactivityTimer = new Timer(requesterInactivityTimeout);
                myResponseReceiverInactivityTimer.AutoReset = false;
                myResponseReceiverInactivityTimer.Elapsed += OnConnectionCheckTimer;

                myResponseReceiverInactivityTimeout = requesterInactivityTimeout;
            }
        }


        public void ProcessRequestResponse(Stream requestMessage, Stream responseMessages)
        {
            using (EneterTrace.Entering())
            {
                // Read the message to know if it is message to be sent or if it is
                // a request to poll responsed messages.
                object aMessage = null;
                try
                {
                    // If the message is not in the memory stream then
                    // because of performance, first put it to the memory stream and then read.
                    if (requestMessage is MemoryStream == false)
                    {
                        using (MemoryStream aMemoryStream = new MemoryStream())
                        {
                            int aSize = 0;
                            byte[] aBuffer = new byte[32768];
                            while ((aSize = requestMessage.Read(aBuffer, 0, aBuffer.Length)) != 0)
                            {
                                aMemoryStream.Write(aBuffer, 0, aSize);
                            }

                            aMemoryStream.Position = 0;
                            aMessage = MessageStreamer.ReadMessage(aMemoryStream);
                        }
                    }
                    else
                    {
                        MemoryStream aMemoryStream = (MemoryStream)requestMessage;
                        aMessage = MessageStreamer.ReadMessage(aMemoryStream);
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to read the incoming request message.", err);
                    throw;
                }

                // Nothing to do
                if (aMessage == null)
                {
                    EneterTrace.Warning(TracedObject + "received a null message.");
                    return;
                }

                // If the message means that the response messages shall be polled.
                if (MessageStreamer.IsPollResponseMessage(aMessage))
                {
                    string aResponseReceiverId = (string)aMessage;

                    lock (myConnections)
                    {
                        try
                        {
                            TConnection aRequestReceiver = myConnections.FirstOrDefault(x => x.ResponseReceiverId == aResponseReceiverId);
                            if (aRequestReceiver == null)
                            {
                                // no responses stored for the receiver
                                return;
                            }

                            aRequestReceiver.LastActivityTime = DateTime.Now;

                            // If there are messages for the receiver
                            if (aRequestReceiver.Messages.Count > 0)
                            {
                                using (MemoryStream aStreamedResponses = new MemoryStream())
                                {
                                    // Dequeue responses to be sent to the response receiver.
                                    // Note: Try not to exceed maximum response size
                                    while (aRequestReceiver.Messages.Count > 0 && aStreamedResponses.Length < myMaximumSizeOfOneResponse)
                                    {
                                        object aResponse = aRequestReceiver.Messages.Dequeue();
                                        MessageStreamer.WriteMessage(aStreamedResponses, aResponse);
                                    }

                                    // Put the memory stream to the output stream.
                                    aStreamedResponses.WriteTo(responseMessages);
                                }
                            }
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Error(TracedObject + "failed to return response messages.", err);
                            throw;
                        }
                    }
                }
                else if (MessageStreamer.IsOpenConnectionMessage(aMessage))
                {
                    object[] aStructuredMessage = (object[])aMessage;
                    string aResponseReceiverId = (string)aStructuredMessage[1];
                    lock (myConnections)
                    {
                        // Try to find if it is an already open connection.
                        TConnection aRequestReceiver = myConnections.FirstOrDefault(x => x.ResponseReceiverId == aResponseReceiverId);
                        if (aRequestReceiver == null)
                        {
                            IDuplexOutputChannel aDuplexOutputChannel = myOutputMessagingFactory.CreateDuplexOutputChannel(myChannelId, aResponseReceiverId);
                            aDuplexOutputChannel.ResponseMessageReceived += OnResponseMessageReceived;

                            try
                            {
                                aDuplexOutputChannel.OpenConnection();
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Error(TracedObject + ErrorHandler.OpenConnectionFailure, err);

                                aDuplexOutputChannel.ResponseMessageReceived -= OnResponseMessageReceived;

                                throw;
                            }

                            aRequestReceiver = new TConnection(aDuplexOutputChannel, aResponseReceiverId);
                            myConnections.Add(aRequestReceiver);

                            myResponseReceiverInactivityTimer.Enabled = true;
                        }
                    }
                }
                else if (MessageStreamer.IsRequestMessage(aMessage))
                {
                    object[] aStructuredMessage = (object[])aMessage;
                    string aResponseReceiverId = (string)aStructuredMessage[1];
                    lock (myConnections)
                    {
                        // if the connection is not open
                        TConnection aRequestReceiver = myConnections.FirstOrDefault(x => x.ResponseReceiverId == aResponseReceiverId);
                        if (aRequestReceiver == null)
                        {
                            string anErrorMsg = TracedObject + ErrorHandler.SendMessageNotConnectedFailure;
                            EneterTrace.Error(anErrorMsg);
                            throw new InvalidOperationException(anErrorMsg);
                        }

                        // Update time of the last activity.
                        aRequestReceiver.LastActivityTime = DateTime.Now;

                        try
                        {
                            // Send the message to the duplex output channel.
                            aRequestReceiver.DuplexOutputChannel.SendMessage(aStructuredMessage[2]);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Error(TracedObject + ErrorHandler.SendMessageFailure, err);
                            throw;
                        }
                    }
                }
                else if (MessageStreamer.IsCloseConnectionMessage(aMessage))
                {
                    string aResponseReceiverId = (string)((object[])aMessage)[1];
                    lock (myConnections)
                    {
                        TConnection aRequestReceiver = myConnections.FirstOrDefault(x => x.ResponseReceiverId == aResponseReceiverId);
                        if (aRequestReceiver != null)
                        {
                            try
                            {
                                aRequestReceiver.DuplexOutputChannel.CloseConnection();
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.CloseConnectionFailure, err);
                            }
                            finally
                            {
                                aRequestReceiver.DuplexOutputChannel.ResponseMessageReceived -= OnResponseMessageReceived;
                            }

                            myConnections.RemoveWhere(x => x.DuplexOutputChannel.IsConnected == false);
                        }
                    }
                }
            }
        }


        private void OnResponseMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnections)
                {
                    TConnection aConnection = myConnections.FirstOrDefault(x => x.DuplexOutputChannel == (IDuplexOutputChannel)sender);
                    if (aConnection != null)
                    {
                        aConnection.Messages.Enqueue(e.Message);
                    }
                    else
                    {
                        EneterTrace.Error(TracedObject + "failed to enqueue the response message because the connection is not open or was closed.");
                    }
                }
            }
        }


        private void OnConnectionCheckTimer(object sender, ElapsedEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnections)
                {
                    foreach (TConnection aConnection in myConnections)
                    {
                        if (DateTime.Now - aConnection.LastActivityTime > TimeSpan.FromMilliseconds(myResponseReceiverInactivityTimeout))
                        {
                            try
                            {
                                // Disconnect
                                aConnection.DuplexOutputChannel.CloseConnection();
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.CloseConnectionFailure, err);
                            }
                            finally
                            {
                                aConnection.DuplexOutputChannel.ResponseMessageReceived -= OnResponseMessageReceived;
                            }
                        }
                    }

                    // Remove all close connections.
                    myConnections.RemoveWhere(x => x.DuplexOutputChannel.IsConnected == false);

                    if (myConnections.Count > 0)
                    {
                        myResponseReceiverInactivityTimer.Enabled = true;
                    }
                }
            }
        }


        private IMessagingSystemFactory myOutputMessagingFactory;
        private string myChannelId = "";

        private HashSet<TConnection> myConnections = new HashSet<TConnection>();

        private int myResponseReceiverInactivityTimeout;
        private int myMaximumSizeOfOneResponse;

        private Timer myResponseReceiverInactivityTimer;


        private string TracedObject
        {
            get
            {
                return "The Bridge connecting the duplex input channel '" + myChannelId + "' ";
            }
        }
    }
}

#endif