﻿/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.Infrastructure.Attachable
{
    internal abstract class AttachableDuplexOutputChannelBase : IAttachableDuplexOutputChannel
    {
        public virtual void AttachDuplexOutputChannel(IDuplexOutputChannel duplexOutputChannel)
        {
            lock (myDuplexOutputChannelManipulatorLock)
            {
                Attach(duplexOutputChannel);

                try
                {
                    AttachedDuplexOutputChannel.OpenConnection();
                }
                catch (Exception err)
                {
                    try
                    {
                        DetachDuplexOutputChannel();
                    }
                    catch
                    {
                        // Ignore exception in exception.
                    }

                    string aMessage = TracedObject + ErrorHandler.OpenConnectionFailure;
                    EneterTrace.Error(aMessage, err);
                    throw new InvalidOperationException(aMessage);
                }
            }
        }

        public virtual void DetachDuplexOutputChannel()
        {
            using (EneterTrace.Entering())
            {
                lock (myDuplexOutputChannelManipulatorLock)
                {
                    if (AttachedDuplexOutputChannel != null)
                    {
                        try
                        {
                            // Try to notify, the connection is closed.
                            AttachedDuplexOutputChannel.CloseConnection();
                        }
                        finally
                        {
                            // Detach the event handler.
                            AttachedDuplexOutputChannel.ResponseMessageReceived -= OnResponseMessageReceived;
                            AttachedDuplexOutputChannel = null;
                        }
                    }
                }
            }
        }

        public virtual bool IsDuplexOutputChannelAttached
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    lock (myDuplexOutputChannelManipulatorLock)
                    {
                        return AttachedDuplexOutputChannel != null;
                    }
                }
            }
        }

        private void Attach(IDuplexOutputChannel duplexOutputChannel)
        {
            using (EneterTrace.Entering())
            {
                lock (myDuplexOutputChannelManipulatorLock)
                {
                    if (duplexOutputChannel == null)
                    {
                        string aMessage = TracedObject + "failed to attach the duplex output channel, because the input parameter 'duplexOutputChannel' is null.";
                        EneterTrace.Error(aMessage);
                        throw new ArgumentNullException(aMessage);
                    }

                    if (string.IsNullOrEmpty(duplexOutputChannel.ChannelId))
                    {
                        string aMessage = TracedObject + "failed to attach the duplex output channel, because the input parameter 'duplexOutputChannel' has empty or null channel id.";
                        EneterTrace.Error(aMessage);
                        throw new ArgumentNullException(aMessage);
                    }

                    if (IsDuplexOutputChannelAttached)
                    {
                        string aMessage = TracedObject + "failed to attach the duplex output channel '" + duplexOutputChannel.ChannelId + "' because the duplex output channel is already attached.";
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    AttachedDuplexOutputChannel = duplexOutputChannel;
                    AttachedDuplexOutputChannel.ResponseMessageReceived += OnResponseMessageReceived;
                }
            }
        }


        protected abstract void OnResponseMessageReceived(object sender, DuplexChannelMessageEventArgs e);


        protected object myDuplexOutputChannelManipulatorLock = new object();
        public IDuplexOutputChannel AttachedDuplexOutputChannel { get; private set; }


        protected abstract string TracedObject { get; }
    }
}
