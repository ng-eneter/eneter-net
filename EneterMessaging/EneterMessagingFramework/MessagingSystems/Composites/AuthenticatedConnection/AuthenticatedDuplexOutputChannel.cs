/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2014
*/

using System;
using System.Threading;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;
using Eneter.Messaging.Threading.Dispatching;

namespace Eneter.Messaging.MessagingSystems.Composites.AuthenticatedConnection
{
    internal class AuthenticatedDuplexOutputChannel : IDuplexOutputChannel
    {
        public event EventHandler<DuplexChannelEventArgs> ConnectionOpened;

        public event EventHandler<DuplexChannelEventArgs> ConnectionClosed;

        public event EventHandler<DuplexChannelMessageEventArgs> ResponseMessageReceived;


        public AuthenticatedDuplexOutputChannel(IDuplexOutputChannel underlyingOutputChannel,
            GetLoginMessage getLoginMessageCallback,
            GetHandshakeResponseMessage getHandshakeResponseMessageCallback,
            TimeSpan authenticationTimeout)
        {
            using (EneterTrace.Entering())
            {
                myUnderlyingOutputChannel = underlyingOutputChannel;
                myGetLoginMessageCallback = getLoginMessageCallback;
                myGetHandshakeResponseMessageCallback = getHandshakeResponseMessageCallback;
                myAuthenticationTimeout = authenticationTimeout;

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

                    try
                    {
                        // Reset internal states.
                        myIsHandshakeResponseSent = false;
                        myIsConnectionAcknowledged = false;
                        myConnectionAcknowledged.Reset();

                        myUnderlyingOutputChannel.OpenConnection();

                        // Send the login message.
                        object aLoginMessage;
                        try
                        {
                            aLoginMessage = myGetLoginMessageCallback(ChannelId, ResponseReceiverId);
                        }
                        catch (Exception err)
                        {
                            string anErrorMessage = TracedObject + "failed to get the login message.";
                            EneterTrace.Error(anErrorMessage, err);
                            throw;
                        }

                        try
                        {
                            myUnderlyingOutputChannel.SendMessage(aLoginMessage);
                        }
                        catch (Exception err)
                        {
                            string anErrorMessage = TracedObject + "failed to send the login message.";
                            EneterTrace.Error(anErrorMessage, err);
                            throw;
                        }

                        // Wait until the hanshake is completed.
                        if (!myConnectionAcknowledged.WaitOne((int)myAuthenticationTimeout.TotalMilliseconds))
                        {
                            string anErrorMessage = TracedObject + "failed to process authentication within defined timeout " + myAuthenticationTimeout.TotalMilliseconds + " ms.";
                            EneterTrace.Error(anErrorMessage);
                            throw new TimeoutException(anErrorMessage);
                        }

                        if (!IsConnected)
                        {
                            String anErrorMessage = TracedObject + ErrorHandler.OpenConnectionFailure;
                            EneterTrace.Error(anErrorMessage);
                            throw new InvalidOperationException(anErrorMessage);
                        }
                    }
                    catch
                    {
                        CloseConnection();
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
                    myUnderlyingOutputChannel.CloseConnection();
                }
            }
        }

        public bool IsConnected
        {
            get
            {
                lock (myConnectionManipulatorLock)
                {
                    return myIsConnectionAcknowledged && myUnderlyingOutputChannel.IsConnected;
                }
            }
        }

        public IThreadDispatcher Dispatcher { get { return myUnderlyingOutputChannel.Dispatcher; } }

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


        private void OnConnectionClosed(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                // If the connection was authenticated then notify that it was closed.
                bool aCloseNotifyFlag = myIsConnectionAcknowledged;

                // If there is waiting for connection open release it.
                myConnectionAcknowledged.Set();
                lock (myConnectionManipulatorLock)
                {
                    myIsHandshakeResponseSent = false;
                    myIsConnectionAcknowledged = false;
                }

                if (aCloseNotifyFlag)
                {

                    Notify<DuplexChannelEventArgs>(ConnectionClosed, e, false);
                }
            }
        }

        // Note: This method is called in the thread defined in used ThreadDispatcher.
        //       So it is the "correct" thread.
        private void OnResponseMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (myIsConnectionAcknowledged)
                {
                    // If the connection is properly established via handshaking.
                    Notify<DuplexChannelMessageEventArgs>(ResponseMessageReceived, e, true);
                    return;
                }

                bool aCloseConnectionFlag = false;

                if (myIsHandshakeResponseSent)
                {
                    // If the handshake was sent then this message must be acknowledgement.
                    string anAcknowledgeMessage = e.Message as string;

                    // If the acknowledge message is wrong then disconnect.
                    if (String.IsNullOrEmpty(anAcknowledgeMessage) || anAcknowledgeMessage != "OK")
                    {
                        string anErrorMessage = TracedObject + "detected incorrect acknowledge message. The connection will be closed.";
                        EneterTrace.Error(anErrorMessage);

                        aCloseConnectionFlag = true;
                    }
                    else
                    {
                        myIsConnectionAcknowledged = true;
                        myConnectionAcknowledged.Set();

                        // Notify the connection is open.
                        DuplexChannelEventArgs anEventArgs = new DuplexChannelEventArgs(ChannelId, ResponseReceiverId, e.SenderAddress);
                        Notify<DuplexChannelEventArgs>(ConnectionOpened, anEventArgs, false);
                    }
                }
                else
                    // This is the handshake message.
                {
                    // Get the response for the handshake message.
                    object aHandshakeResponseMessage = null;
                    try
                    {
                        aHandshakeResponseMessage = myGetHandshakeResponseMessageCallback(e.ChannelId, e.ResponseReceiverId, e.Message);
                        aCloseConnectionFlag = (aHandshakeResponseMessage == null);
                    }
                    catch (Exception err)
                    {
                        string anErrorMessage = TracedObject + "failed to get the handshake response message. The connection will be closed.";
                        EneterTrace.Error(anErrorMessage, err);

                        aCloseConnectionFlag = true;
                    }

                    // Send back the response for the handshake.
                    if (!aCloseConnectionFlag)
                    {
                        try
                        {
                            // Note: keep setting this flag before sending. Otherwise synchronous messaging will not work!
                            myIsHandshakeResponseSent = true;
                            myUnderlyingOutputChannel.SendMessage(aHandshakeResponseMessage);
                        }
                        catch (Exception err)
                        {
                            myIsHandshakeResponseSent = false;

                            string anErrorMessage = TracedObject + "failed to send the handshake response message. The connection will be closed.";
                            EneterTrace.Error(anErrorMessage, err);

                            aCloseConnectionFlag = true;
                        }
                    }
                }

                if (aCloseConnectionFlag)
                {
                    myUnderlyingOutputChannel.CloseConnection();
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
        private GetLoginMessage myGetLoginMessageCallback;
        private GetHandshakeResponseMessage myGetHandshakeResponseMessageCallback;
        private TimeSpan myAuthenticationTimeout;
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
