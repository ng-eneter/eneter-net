/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.Composites.ReliableMessagingComposit;

namespace Eneter.Messaging.Infrastructure.Attachable
{
    internal abstract class AttachableReliableOutputChannelBase : AttachableDuplexOutputChannelBase, IAttachableReliableOutputChannel
    {
        public event EventHandler<MessageIdEventArgs> MessageDelivered;

        public event EventHandler<MessageIdEventArgs> MessageNotDelivered;


        public void AttachReliableOutputChannel(IReliableDuplexOutputChannel reliableDuplexOutputChannel)
        {
            using (EneterTrace.Entering())
            {
                lock (myDuplexOutputChannelManipulatorLock)
                {
                    reliableDuplexOutputChannel.MessageDelivered += OnMessageDelivered;
                    reliableDuplexOutputChannel.MessageNotDelivered += OnMessageNotDelivered;

                    try
                    {
                        base.AttachDuplexOutputChannel(reliableDuplexOutputChannel);
                    }
                    catch (Exception err)
                    {
                        reliableDuplexOutputChannel.MessageDelivered -= OnMessageDelivered;
                        reliableDuplexOutputChannel.MessageNotDelivered -= OnMessageNotDelivered;

                        EneterTrace.Error(TracedObject + "failed to attach reliable duplex output channel.", err);
                        throw;
                    }
                }
            }
        }



        public void DetachReliableOutputChannel()
        {
            using (EneterTrace.Entering())
            {
                lock (myDuplexOutputChannelManipulatorLock)
                {
                    IReliableDuplexOutputChannel aDetachedDuplexOutputChannel = AttachedReliableOutputChannel;

                    try
                    {
                        base.DetachDuplexOutputChannel();
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + "failed to detach reliable duplex output channel.", err);
                    }

                    if (aDetachedDuplexOutputChannel != null)
                    {
                        aDetachedDuplexOutputChannel.MessageDelivered -= OnMessageDelivered;
                        aDetachedDuplexOutputChannel.MessageNotDelivered -= OnMessageNotDelivered;
                    }
                }
            }
        }


        public bool IsReliableOutputChannelAttached
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    lock (myDuplexOutputChannelManipulatorLock)
                    {
                        return base.IsDuplexOutputChannelAttached;
                    }
                }
            }
        }

        public IReliableDuplexOutputChannel AttachedReliableOutputChannel
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    return base.AttachedDuplexOutputChannel as IReliableDuplexOutputChannel;
                }
            }
        }

        private void OnMessageDelivered(object sender, MessageIdEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (MessageDelivered != null)
                {
                    try
                    {
                        MessageDelivered(this, e);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }
        
        private void OnMessageNotDelivered(object sender, MessageIdEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (MessageNotDelivered != null)
                {
                    try
                    {
                        MessageNotDelivered(this, e);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }
    }
}
