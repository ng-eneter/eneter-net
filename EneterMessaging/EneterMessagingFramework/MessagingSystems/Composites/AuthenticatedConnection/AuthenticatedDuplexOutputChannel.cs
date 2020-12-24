

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
            TimeSpan authenticationTimeout,
            IThreadDispatcher threadDispatcher)
        {
            using (EneterTrace.Entering())
            {
                myUnderlyingOutputChannel = underlyingOutputChannel;
                myGetLoginMessageCallback = getLoginMessageCallback;
                myGetHandshakeResponseMessageCallback = getHandshakeResponseMessageCallback;
                myAuthenticationTimeout = authenticationTimeout;

                myUnderlyingOutputChannel.ConnectionClosed += OnConnectionClosed;
                myUnderlyingOutputChannel.ResponseMessageReceived += OnResponseMessageReceived;

                myThreadDispatcher = threadDispatcher;
            }
        }

        public string ChannelId { get { return myUnderlyingOutputChannel.ChannelId; } }

        public string ResponseReceiverId { get { return myUnderlyingOutputChannel.ResponseReceiverId; } }
        

        public void OpenConnection()
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myConnectionManipulatorLock))
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
                        myAuthenticationEnded.Reset();

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

                        // Wait until the handshake is completed.
                        if (!myAuthenticationEnded.WaitOne((int)myAuthenticationTimeout.TotalMilliseconds))
                        {
                            string anErrorMessage = TracedObject + "failed to process authentication within defined timeout " + myAuthenticationTimeout.TotalMilliseconds + " ms.";
                            EneterTrace.Error(anErrorMessage);
                            throw new TimeoutException(anErrorMessage);
                        }

                        if (!IsConnected)
                        {
                            string anErrorMessage = TracedObject + "failed to authenticate '" + aLoginMessage + "'.";
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
                using (ThreadLock.Lock(myConnectionManipulatorLock))
                {
                    myUnderlyingOutputChannel.CloseConnection();
                }
            }
        }

        public bool IsConnected
        {
            get
            {
                using (ThreadLock.Lock(myConnectionManipulatorLock))
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
                using (ThreadLock.Lock(myConnectionManipulatorLock))
                {
                    if (!IsConnected)
                    {
                        string aMessage = TracedObject + ErrorHandler.FailedToSendMessageBecauseNotConnected;
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
                // If there is waiting for connection open release it.
                myAuthenticationEnded.Set();

                // If the connection was authenticated then notify that it was closed.
                if (myIsConnectionAcknowledged)
                {
                    myThreadDispatcher.Invoke(() => Notify<DuplexChannelEventArgs>(ConnectionClosed, e, false));
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
                    myThreadDispatcher.Invoke(() => Notify<DuplexChannelMessageEventArgs>(ResponseMessageReceived, e, true));
                    return;
                }

                bool aCloseConnectionFlag = false;

                // This is the handshake message.
                if (!myIsHandshakeResponseSent)
                {
                    EneterTrace.Debug("HANDSHAKE RECEIVED");

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
                        EneterTrace.Warning(anErrorMessage, err);

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
                else
                {
                    EneterTrace.Debug("CONNECTION ACKNOWLEDGE RECEIVED");

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
                        myAuthenticationEnded.Set();

                        // Notify the connection is open.
                        DuplexChannelEventArgs anEventArgs = new DuplexChannelEventArgs(ChannelId, ResponseReceiverId, e.SenderAddress);
                        myThreadDispatcher.Invoke(() => Notify<DuplexChannelEventArgs>(ConnectionOpened, anEventArgs, false));
                    }
                }

                if (aCloseConnectionFlag)
                {
                    myUnderlyingOutputChannel.CloseConnection();

                    // Release the waiting in OpenConnection(..).
                    myAuthenticationEnded.Set();
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


        private IThreadDispatcher myThreadDispatcher;
        private IDuplexOutputChannel myUnderlyingOutputChannel;
        private GetLoginMessage myGetLoginMessageCallback;
        private GetHandshakeResponseMessage myGetHandshakeResponseMessageCallback;
        private TimeSpan myAuthenticationTimeout;
        private bool myIsHandshakeResponseSent;
        private bool myIsConnectionAcknowledged;
        private ManualResetEvent myAuthenticationEnded = new ManualResetEvent(false);
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
