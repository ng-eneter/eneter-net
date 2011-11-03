/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase
{
    /// <summary>
    /// Internal basic implementation of the input channel.
    /// </summary>
    internal class SimpleInputChannel : IInputChannel
    {
        public event EventHandler<ChannelMessageEventArgs> MessageReceived;

        public SimpleInputChannel(string channelId, IMessagingSystemBase messagingSystem)
        {
            using (EneterTrace.Entering())
            {
                if (string.IsNullOrEmpty(channelId))
                {
                    EneterTrace.Error(ErrorHandler.NullOrEmptyChannelId);
                    throw new ArgumentException(ErrorHandler.NullOrEmptyChannelId);
                }

                ChannelId = channelId;
                MessagingSystem = messagingSystem;
            }
        }

        public string ChannelId { get; private set; }

        /// <summary>
        /// Registers the delegate in the messaging system to receive messages from a desired channel id and starts the listening.
        /// </summary>
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
                        MessagingSystem.RegisterMessageHandler(ChannelId, (Action<object>)HandleMessage);
                        myIsListeningFlag = true;
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.StartListeningFailure, err);

                        try
                        {
                            StopListening();
                        }
                        catch
                        {
                            // We tried to clean after the failure.
                            // We can ignore this exception.
                        }

                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Unregisters the channel id from the listening.
        /// </summary>
        public void StopListening()
        {
            using (EneterTrace.Entering())
            {
                lock (myListeningManipulatorLock)
                {
                    try
                    {
                        MessagingSystem.UnregisterMessageHandler(ChannelId);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.StopListeningFailure, err);
                    }

                    myIsListeningFlag = false;
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
                        return myIsListeningFlag;
                    }
                }
            } 
        }
        
        /// <summary>
        /// Handles the received message from the message system.
        /// </summary>
        private void HandleMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                if (MessageReceived != null)
                {
                    try
                    {
                        MessageReceived(this, new ChannelMessageEventArgs(ChannelId, message));
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

        private IMessagingSystemBase MessagingSystem { get; set; }

        private volatile bool myIsListeningFlag;

        private object myListeningManipulatorLock = new object();


        private string TracedObject
        {
            get
            {
                return "The input channel '" + ChannelId + "' ";
            }
        }

    }
}
