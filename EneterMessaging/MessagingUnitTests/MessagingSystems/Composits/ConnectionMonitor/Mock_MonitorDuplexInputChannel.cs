using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.Composites.MonitoredMessagingComposit;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.MessagingUnitTests.MessagingSystems.Composits.ConnectionMonitor
{
    internal class Mock_MonitorDuplexInputChannel : IDuplexInputChannel
    {
        public event EventHandler<ConnectionTokenEventArgs> ResponseReceiverConnecting;
        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverConnected;
        public event EventHandler<ResponseReceiverEventArgs> ResponseReceiverDisconnected;
        public event EventHandler<DuplexChannelMessageEventArgs> MessageReceived;


        public Mock_MonitorDuplexInputChannel(IDuplexInputChannel underlyingInputChannel, ISerializer serializer)
        {
            myUnderlyingInputChannel = underlyingInputChannel;
            myUnderlyingInputChannel.ResponseReceiverConnecting += OnResponseReceiverConnecting;
            myUnderlyingInputChannel.ResponseReceiverConnected += OnResponseReceiverConnected;
            myUnderlyingInputChannel.ResponseReceiverDisconnected += OnResponseReceiverDisconnected;
            myUnderlyingInputChannel.MessageReceived += OnMessageReceived;

            mySerializer = serializer;

            ResponsePingFlag = true;
        }


        public string ChannelId
        {
            get { return myUnderlyingInputChannel.ChannelId; }
        }

        public IDispatcher Dispatcher
        {
            get { return myUnderlyingInputChannel.Dispatcher; }
        }

        public void StartListening()
        {
            myUnderlyingInputChannel.StartListening();
        }

        public void StopListening()
        {
            myUnderlyingInputChannel.StopListening();
        }

        public bool IsListening
        {
            get { return myUnderlyingInputChannel.IsListening; }
        }

        public void SendResponseMessage(string responseReceiverId, object message)
        {
            // Create the response message for the monitor duplex output chanel.
            MonitorChannelMessage aMessage = new MonitorChannelMessage(MonitorChannelMessageType.Message, message);
            object aSerializedMessage = mySerializer.Serialize<MonitorChannelMessage>(aMessage);

            // Send the response message via the underlying channel.
            myUnderlyingInputChannel.SendResponseMessage(responseReceiverId, aSerializedMessage);
        }

        public void DisconnectResponseReceiver(string responseReceiverId)
        {
            myUnderlyingInputChannel.DisconnectResponseReceiver(responseReceiverId);
        }

        public bool ResponsePingFlag { get; set; }


        private void OnResponseReceiverConnecting(object sender, ConnectionTokenEventArgs e)
        {
            if (ResponseReceiverConnecting != null)
            {
                try
                {
                    ResponseReceiverConnecting(this, e);
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + "detected an exception from the 'ResponseReceiverConnecting' event handler.", err);
                }
            }
        }

        private void OnResponseReceiverConnected(object sender, ResponseReceiverEventArgs e)
        {
            if (ResponseReceiverConnected != null)
            {
                try
                {
                    ResponseReceiverConnected(this, e);
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + "detected an exception from the 'ResponseReceiverConnected' event handler.", err);
                }
            }
        }

        private void OnResponseReceiverDisconnected(object sender, ResponseReceiverEventArgs e)
        {
            if (ResponseReceiverDisconnected!= null)
            {
                try
                {
                    ResponseReceiverDisconnected(this, e);
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + "detected an exception from the 'ResponseReceiverDisconnected' event handler.", err);
                }
            }
        }

        private void OnMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            try
            {
                // Deserialize the incoming message.
                MonitorChannelMessage aMessage = mySerializer.Deserialize<MonitorChannelMessage>(e.Message);

                // if the message is ping, then response.
                if (aMessage.MessageType == MonitorChannelMessageType.Ping)
                {
                    EneterTrace.Info(TracedObject + "received the ping.");

                    try
                    {
                        if (ResponsePingFlag)
                        {
                            myUnderlyingInputChannel.SendResponseMessage(e.ResponseReceiverId, e.Message);

                            EneterTrace.Info(TracedObject + "responded the ping.");
                        }
                        else
                        {
                            EneterTrace.Info(TracedObject + "did not response the ping.");
                        }
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + "failed to response to ping message.", err);
                    }
                }
                else
                {
                    // Notify the incoming message.
                    if (MessageReceived != null)
                    {
                        DuplexChannelMessageEventArgs aMsg = new DuplexChannelMessageEventArgs(e.ChannelId, aMessage.MessageContent, e.ResponseReceiverId, e.SenderAddress);

                        try
                        {
                            MessageReceived(this, aMsg);
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + "detected an exception from the 'MessageReceived' event handler.", err);
                        }
                    }
                }
            }
            catch (Exception err)
            {
                EneterTrace.Error(TracedObject + "failed to receive the message.", err.Message);
            }
        }


        private IDuplexInputChannel myUnderlyingInputChannel;
        private ISerializer mySerializer;

        private string TracedObject
        {
            get
            {
                string aChannelId = (myUnderlyingInputChannel != null) ? myUnderlyingInputChannel.ChannelId : "";
                return "The MOCK monitor duplex input channel '" + aChannelId + "' ";
            }
        }


    }
}
