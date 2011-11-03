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
    internal abstract class AttachableOutputChannelBase : IAttachableOutputChannel
    {
        public virtual void AttachOutputChannel(IOutputChannel outputChannel)
        {
            using (EneterTrace.Entering())
            {
                lock (this)
                {
                    if (IsOutputChannelAttached)
                    {
                        string aMessage = "The output channel is already attached. The currently attached channel id is '" + AttachedOutputChannel.ChannelId + "'.";
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    AttachedOutputChannel = outputChannel;
                }
            }
        }

        public virtual void DetachOutputChannel()
        {
            using (EneterTrace.Entering())
            {
                lock (this)
                {
                    AttachedOutputChannel = null;
                }
            }
        }

        public virtual bool IsOutputChannelAttached
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    lock (this)
                    {
                        return AttachedOutputChannel != null;
                    }
                }
            }
        }

        public IOutputChannel AttachedOutputChannel { get; private set; }
    }
}
