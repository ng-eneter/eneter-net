/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using System.Collections.Generic;
using System.Linq;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.Nodes.ChannelWrapper
{
    internal sealed class DuplexChannelUnwrapper : AttachableDuplexInputChannelBase, IDuplexChannelUnwrapper
    {
        private class TDuplexConnection
        {
            public TDuplexConnection(string responseReceiverId, IDuplexOutputChannel duplexOutputChannel)
            {
                ResponseReceiverId = responseReceiverId;
                DuplexOutputChannel = duplexOutputChannel;
            }

            public string ResponseReceiverId { get; private set; }
            public IDuplexOutputChannel DuplexOutputChannel { get; private set; }
        }

        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;

        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;



        public DuplexChannelUnwrapper(IMessagingSystemFactory outputMessagingFactory, ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                myOutputMessagingFactory = outputMessagingFactory;
                mySerializer = serializer;
            }
        }

        public string GetAssociatedResponseReceiverId(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnections)
                {
                    TDuplexConnection aConnection = myConnections.FirstOrDefault(x => x.DuplexOutputChannel.ResponseReceiverId == responseReceiverId);
                    if (aConnection != null)
                    {
                        return aConnection.ResponseReceiverId;
                    }

                    return null;
                }
            }
        }

        protected override void OnRequestMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                WrappedData aWrappedData = null;

                try
                {
                    // Unwrap the incoming message.
                    aWrappedData = DataWrapper.Unwrap(e.Message, mySerializer);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to unwrap the message.", err);
                    return;
                }

                // WrappedData.AddedData represents the channel id.
                // Therefore if everything is ok then it must be string.
                if (aWrappedData.AddedData is string)
                {
                    string aMessageReceiverId = (string)aWrappedData.AddedData;

                    TDuplexConnection aConectionToOutput = null;

                    // Try to find if the output channel with the required channel id and for the incoming response receiver
                    // already exists.
                    lock (myConnections)
                    {
                        aConectionToOutput = myConnections.FirstOrDefault(x => x.DuplexOutputChannel.ChannelId == aMessageReceiverId && x.ResponseReceiverId == e.ResponseReceiverId);

                        // If it does not exist then create the duplex output channel and open connection.
                        if (aConectionToOutput == null)
                        {
                            IDuplexOutputChannel aDuplexOutputChannel = null;

                            try
                            {
                                aDuplexOutputChannel = myOutputMessagingFactory.CreateDuplexOutputChannel(aMessageReceiverId);
                                aDuplexOutputChannel.ResponseMessageReceived += OnResponseMessageReceived;
                                aDuplexOutputChannel.OpenConnection();
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Error(TracedObject + "failed to create and connect the duplex output channel '" + aMessageReceiverId + "'", err);

                                if (aDuplexOutputChannel != null)
                                {
                                    aDuplexOutputChannel.ResponseMessageReceived -= OnResponseMessageReceived;
                                    aDuplexOutputChannel.CloseConnection();
                                    aDuplexOutputChannel = null;
                                }
                            }

                            if (aDuplexOutputChannel != null)
                            {
                                aConectionToOutput = new TDuplexConnection(e.ResponseReceiverId, aDuplexOutputChannel);
                                myConnections.Add(aConectionToOutput);
                            }
                        }
                    }

                    if (aConectionToOutput != null)
                    {
                        try
                        {
                            // Send the unwrapped message.
                            aConectionToOutput.DuplexOutputChannel.SendMessage(aWrappedData.OriginalData);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Error(TracedObject + "failed to send the message to the output channel '" + aConectionToOutput.DuplexOutputChannel.ChannelId + "'.", err);
                        }
                    }
                }
                else
                {
                    EneterTrace.Error(TracedObject + "detected that the unwrapped message contian the channel id as the string type.");
                }
            }
        }

        /// <summary>
        /// The method is called when the response receiver is disconnected.
        /// The method clears all connections related to the disconnected receiver.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void OnResponseReceiverDisconnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnections)
                {
                    IEnumerable<TDuplexConnection> aConnections = myConnections.Where(x => x.ResponseReceiverId == e.ResponseReceiverId);
                    foreach (TDuplexConnection aConnection in aConnections)
                    {
                        try
                        {
                            aConnection.DuplexOutputChannel.ResponseMessageReceived -= OnResponseMessageReceived;
                            aConnection.DuplexOutputChannel.CloseConnection();
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.FailedToCloseConnection, err);
                        }

                    }

                    myConnections.RemoveWhere(x => x.ResponseReceiverId == e.ResponseReceiverId);
                }

                if (ResponseReceiverDisconnected != null)
                {
                    try
                    {
                        ResponseReceiverDisconnected(this, e);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }

        protected override void OnResponseReceiverConnected(object sender, ResponseReceiverEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (ResponseReceiverConnected != null)
                {
                    try
                    {
                        ResponseReceiverConnected(this, e);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }


        /// <summary>
        /// Method is called when a response is received from the duplex output channel.
        /// It wrapps the response and sends the wrapped response to the correct response receiver as the response.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnResponseMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    // try to find the response receiver id where the wrapped message should be responded.
                    TDuplexConnection aConnction = null;
                    lock (myConnections)
                    {
                        aConnction = myConnections.FirstOrDefault(x => x.DuplexOutputChannel == (IDuplexOutputChannel)sender);
                    }

                    if (aConnction != null)
                    {
                        object aMessage = DataWrapper.Wrap(e.ChannelId, e.Message, mySerializer);
                        AttachedDuplexInputChannel.SendResponseMessage(aConnction.ResponseReceiverId, aMessage);
                    }
                    else
                    {
                        EneterTrace.Warning(TracedObject + "failed to send the response message because the response receiver id does not exist. It is possible the response receiver has already been disconnected.");
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.FailedToSendResponseMessage, err);
                }
            }
        }


        private IMessagingSystemFactory myOutputMessagingFactory;
        private ISerializer mySerializer;

        private HashSet<TDuplexConnection> myConnections = new HashSet<TDuplexConnection>();

        protected override string TracedObject
        {
            get
            {
                string aDuplexInputChannelId = (AttachedDuplexInputChannel != null) ? AttachedDuplexInputChannel.ChannelId : "";
                return GetType().Name + " '" + aDuplexInputChannelId + "' ";
            }
        }
    }
}
