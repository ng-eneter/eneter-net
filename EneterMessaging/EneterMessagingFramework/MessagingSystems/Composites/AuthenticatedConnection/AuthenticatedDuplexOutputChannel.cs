using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Threading.Dispatching;
using System.Threading;

namespace Eneter.Messaging.MessagingSystems.Composites.AuthenticatedConnection
{
    internal class AuthenticatedDuplexOutputChannel : IDuplexOutputChannel
    {
        public event EventHandler<DuplexChannelEventArgs> ConnectionOpened;

        public event EventHandler<DuplexChannelEventArgs> ConnectionClosed;

        public event EventHandler<DuplexChannelMessageEventArgs> ResponseMessageReceived;


        public AuthenticatedDuplexOutputChannel(IDuplexOutputChannel underlyingOutputChannel, GetHandshakeResponseCallback handshakeResponseCallback)
        {
            using (EneterTrace.Entering())
            {
                myUnderlyingOutputChannel = underlyingOutputChannel;
                myHandshakeResponseCallback = handshakeResponseCallback;

                myUnderlyingOutputChannel.ConnectionClosed += OnConnectionClosed;
                myUnderlyingOutputChannel.ResponseMessageReceived += OnResponseMessageReceived;
            }
        }

        public string ChannelId { get { return myUnderlyingOutputChannel.ChannelId; } }

        public string ResponseReceiverId { get { return myUnderlyingOutputChannel.ResponseReceiverId; } }
        

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

                    myIsHandshakeResponseSent = false;
                    myUnderlyingOutputChannel.OpenConnection();

                    // Wait until the hanshake is completed.
                    if (!myConnectionAcknowledged.WaitOne(5000))
                    {
                        string aWarningMessage = TracedObject + "detected the handshake message was not processed within 5 seconds.";
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
                    myUnderlyingOutputChannel.CloseConnection();
                    
                    myConnectionAcknowledged.Reset();
                    myIsHandshakeResponseSent = false;
                    myIsConnectionAcknowledged = false;
                }
            }
        }

        public bool IsConnected
        {
            get
            {
                lock (myConnectionManipulatorLock)
                {
                    return myIsHandshakeResponseSent && myUnderlyingOutputChannel.IsConnected;
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

                    myUnderlyingOutputChannel.SendMessage(message);
                }
            }
        }

        public IThreadDispatcher Dispatcher { get { return myUnderlyingOutputChannel.Dispatcher; } }
        


        private void OnConnectionClosed(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    myConnectionAcknowledged.Reset();
                    myIsHandshakeResponseSent = false;
                    myIsConnectionAcknowledged = false;
                }

                if (myIsHandshakeResponseSent)
                {
                    Notify<DuplexChannelEventArgs>(ConnectionClosed, e, false);
                }
            }
        }

        private void OnResponseMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (myIsConnectionAcknowledged)
                {
                    // If the connection is properly established via hadshaking.
                    Notify<DuplexChannelMessageEventArgs>(ResponseMessageReceived, e, true);
                }
                else if (myIsHandshakeResponseSent)
                {
                    // If the handshake was sent then this message must be acknowledgement.
                    string anAcknowledgeMessage = e.Message as string;

                    // If the acknowledge message is wrong then disconnect.
                    if (String.IsNullOrEmpty(anAcknowledgeMessage) || anAcknowledgeMessage != "OK")
                    {
                        string anErrorMessage = TracedObject + "detected incorrect acknowledge message. The connection will be closed.";
                        EneterTrace.Error(anErrorMessage);

                        myUnderlyingOutputChannel.CloseConnection();
                        return;
                    }

                    myIsConnectionAcknowledged = true;
                    myConnectionAcknowledged.Set();

                    // Notify the connection is open.
                    DuplexChannelEventArgs anEventArgs = new DuplexChannelEventArgs(ChannelId, ResponseReceiverId, e.SenderAddress);
                    Notify<DuplexChannelEventArgs>(ConnectionOpened, anEventArgs, false);
                }
                else
                {
                    // Therefore this message must be the handshake message.
                    string aHandshakeMessage = e.Message as string;
                    
                    // If the handshake message is wrong then disconnect.
                    if (String.IsNullOrEmpty(aHandshakeMessage))
                    {
                        string anErrorMessage = TracedObject + "detected incorrect handshake message. The connection will be closed.";
                        EneterTrace.Error(anErrorMessage);

                        myUnderlyingOutputChannel.CloseConnection();
                        return;
                    }

                    // Get the response for the handshake message.
                    object anAuthenticationResponse = null;
                    try
                    {
                        anAuthenticationResponse = myHandshakeResponseCallback(aHandshakeMessage);
                    }
                    catch (Exception err)
                    {
                        string anErrorMessage = TracedObject + "detected exception from callback for handshale response. The connection will be closed.";
                        EneterTrace.Error(anErrorMessage, err);

                        myUnderlyingOutputChannel.CloseConnection();
                        return;
                    }

                    // Send back the response for the handshake.
                    try
                    {
                        myUnderlyingOutputChannel.SendMessage(anAuthenticationResponse);
                    }
                    catch (Exception err)
                    {
                        string anErrorMessage = TracedObject + "failed to send the response for the handshake. The connection will be closed.";
                        EneterTrace.Error(anErrorMessage, err);

                        myUnderlyingOutputChannel.CloseConnection();
                        return;
                    }

                    // The handshake response was sent.
                    // Note: if the response is wrong the input channel will disconnect this output channel.
                    myIsHandshakeResponseSent = true;
                }
            }
        }

        private void Notify<T>(EventHandler<T> handler, T eventArgs, bool isNobodySubscribedWarning)
            where T : EventArgs
        {
            using (EneterTrace.Entering())
            {
                if (handler != null)
                {
                    try
                    {
                        handler(this, eventArgs);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
                else if (isNobodySubscribedWarning)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.NobodySubscribedForMessage);
                }
            }
        }


        private IDuplexOutputChannel myUnderlyingOutputChannel;
        private GetHandshakeResponseCallback myHandshakeResponseCallback;
        private bool myIsHandshakeResponseSent;
        private bool myIsConnectionAcknowledged;
        private ManualResetEvent myConnectionAcknowledged = new ManualResetEvent(false);
        private object myConnectionManipulatorLock = new object();


        private string TracedObject
        {
            get
            {
                return GetType().Name + " '" + ChannelId + "' ";
            }
        }
    }
}
