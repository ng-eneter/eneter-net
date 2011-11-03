/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System;
using System.Threading;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.Composites.MonitoredMessagingComposit
{
    /// <summary>
    /// The helper allowing to observe the particular duplex output channel.
    /// If the channel is disconnected, it tries to reopen the connection.
    /// </summary>
    public class Reconnecter
    {
        /// <summary>
        /// The event is invoked when the observed duplex output channel notified, that the connection was closed.
        /// </summary>
        public event EventHandler<DuplexChannelEventArgs> ConnectionClosed;

        /// <summary>
        /// The event is invoked when the duplex output channel was reconnected.
        /// </summary>
        public event EventHandler<DuplexChannelEventArgs> ConnectionOpened;

        /// <summary>
        /// The event is invoked when the reopenning of the duplex output channel failed.
        /// </summary>
        public event EventHandler<DuplexChannelEventArgs> ReconnectingFailed;

        /// <summary>
        /// Constructs the reconnecter that tries to reconnect infinitely every second.
        /// </summary>
        /// <param name="duplexOutputChannel">observed duplex output channel</param>
        public Reconnecter(IDuplexOutputChannel duplexOutputChannel)
            : this(duplexOutputChannel, TimeSpan.FromMilliseconds(1000), -1)
        {
        }
        
        /// <summary>
        /// Constructs the reconnecter from specified parameters.
        /// </summary>
        /// <param name="duplexOutputChannel">observed duplex output channel</param>
        /// <param name="reconnectFrequency">how often the reconnect attempt shall be performed (in case of the disconnection)</param>
        /// <param name="maxReconnectAttempts">max amounts of reconnect attempts. If exceeded, the ReconnectingFailed is invoked.</param>
        public Reconnecter(IDuplexOutputChannel duplexOutputChannel, TimeSpan reconnectFrequency, int maxReconnectAttempts)
        {
            using (EneterTrace.Entering())
            {
                if (duplexOutputChannel == null)
                {
                    EneterTrace.Error(TracedObject + "detected null input parameter 'duplexOutputChannel'.");
                    throw new ArgumentNullException("The input parameter 'duplexOutputChannel' is null.");
                }

                myDuplexOutputChannel = duplexOutputChannel;
                myReconnecFrequency = reconnectFrequency;
                myMaxReconnectAttempts = maxReconnectAttempts;
            }
        }

        /// <summary>
        /// Enables the automatic reconnecting in case the disconnect is notified.
        /// </summary>
        public void EnableReconnecting()
        {
            using (EneterTrace.Entering())
            {
                lock (myMonitoringManipulatorLock)
                {
                    if (IsReconnectingEnabled)
                    {
                        string anErrorMessage = TracedObject + "failed to activate reconnecting because the reconnecting is already activated.";
                        EneterTrace.Error(anErrorMessage);
                        throw new InvalidOperationException(anErrorMessage);
                    }

                    myDuplexOutputChannel.ConnectionClosed += OnConnectionClosed;

                    myIsMonitoringFlag = true;
                }
            }
        }

        /// <summary>
        /// Disables the automatic reconnecting.
        /// </summary>
        public void DisableReconnecting()
        {
            using (EneterTrace.Entering())
            {
                lock (myMonitoringManipulatorLock)
                {
                    myIsMonitoringFlag = false;

                    myDuplexOutputChannel.ConnectionClosed -= OnConnectionClosed;
                }
            }
        }

        /// <summary>
        /// Returns true if the reconnecting is enabled.
        /// </summary>
        public bool IsReconnectingEnabled
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    lock (myMonitoringManipulatorLock)
                    {
                        return myIsMonitoringFlag;
                    }
                }
            }
        }

        private void OnConnectionClosed(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                // Notify the disconnection.
                if (ConnectionClosed != null)
                {
                    try
                    {
                        ConnectionClosed(this, e);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }

                // In order not to block the thread processing this event handler
                // start the reconnection loop in a different thread.
                WaitCallback aDoReconnect = x => DoReconnect();
                ThreadPool.QueueUserWorkItem(aDoReconnect);
            }
        }

        private void DoReconnect()
        {
            using (EneterTrace.Entering())
            {
                int aReconnectAttempt = 0;

                // While the loop is not asked to stop or the amount of performed reconnect attempts exceeded the max amount of allowed attempts.
                // Note: If the max of allowed attempts is < 0, then the number of reconnect attempts is not limited.
                while (myIsMonitoringFlag &&
                    (myMaxReconnectAttempts > -1) ? aReconnectAttempt < myMaxReconnectAttempts : true)
                {
                    Thread.Sleep(myReconnecFrequency);

                    // Try to reconnect.
                    try
                    {
                        myDuplexOutputChannel.OpenConnection();
                    }
                    catch
                    {
                        // The exception can occur here because of more reasons.
                        // E.g. The opening of connecion failed, but also if the connecion is already open (e.g. if it was open by somebody else
                        //      in a different thred.
                        //      Therefore, it is not important to evaluate the excaption here.
                        //      Important is to evaluate the 'IsConnected' property - see the code after the catch area.
                    }

                    // Increase the number of performed attempts.
                    ++aReconnectAttempt;

                    // If the connection is open.
                    if (myDuplexOutputChannel.IsConnected)
                    {
                        // Notify the reconnect.
                        if (ConnectionOpened != null)
                        {
                            try
                            {
                                DuplexChannelEventArgs e = new DuplexChannelEventArgs(myDuplexOutputChannel.ChannelId, myDuplexOutputChannel.ResponseReceiverId);
                                ConnectionOpened(this, e);
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                            }
                        }

                        // The connection was established, so end the loop.
                        return;
                    }

                    // If the last attempt to reconnect failed, then notify that the reconnecting failed.
                    if (myMaxReconnectAttempts > -1 && aReconnectAttempt >= myMaxReconnectAttempts)
                    {
                        // Notify the reconnect.
                        if (ReconnectingFailed != null)
                        {
                            try
                            {
                                DuplexChannelEventArgs e = new DuplexChannelEventArgs(myDuplexOutputChannel.ChannelId, myDuplexOutputChannel.ResponseReceiverId);
                                ReconnectingFailed(this, e);
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                            }
                        }

                        // The connecting failed, so stop looping and return
                        return;
                    }
                }
            }
        }


        private IDuplexOutputChannel myDuplexOutputChannel;
        private TimeSpan myReconnecFrequency;
        private object myMonitoringManipulatorLock = new object();
        private bool myIsMonitoringFlag;
        private int myMaxReconnectAttempts;

        private string TracedObject
        {
            get
            {
                return "Reconnecter for '" + myDuplexOutputChannel.ChannelId + "' ";
            }
        }
    }
}
