/*
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
    internal abstract class AttachableInputChannelBase : IAttachableInputChannel
    {
        public virtual void AttachInputChannel(IInputChannel inputChannel)
        {
            using (EneterTrace.Entering())
            {
                lock (this)
                {
                    if (IsInputChannelAttached)
                    {
                        string aMessage = "The input channel is already attached.";
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    AttachedInputChannel = inputChannel;

                    AttachedInputChannel.MessageReceived += OnMessageReceived;

                    try
                    {
                        AttachedInputChannel.StartListening();
                    }
                    catch
                    {
                        // Clean after the failure.
                        AttachedInputChannel.MessageReceived -= OnMessageReceived;
                        AttachedInputChannel = null;

                        throw;
                    }
                }
            }
        }

        public virtual void DetachInputChannel()
        {
            using (EneterTrace.Entering())
            {
                lock (this)
                {
                    if (IsInputChannelAttached)
                    {
                        try
                        {
                            if (AttachedInputChannel.IsListening)
                            {
                                AttachedInputChannel.StopListening();
                            }
                        }
                        finally
                        {
                            AttachedInputChannel.MessageReceived -= OnMessageReceived;
                            AttachedInputChannel = null;
                        }
                    }
                }
            }
        }

        public virtual bool IsInputChannelAttached
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    lock (this)
                    {
                        return AttachedInputChannel != null;
                    }
                }
            }
        }

        public IInputChannel AttachedInputChannel { get; private set; }

        protected abstract void OnMessageReceived(object sender, ChannelMessageEventArgs e);
    }
}
