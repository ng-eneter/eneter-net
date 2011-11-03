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
    internal abstract class AttachableReliableInputChannelBase : AttachableDuplexInputChannelBase, IAttachableReliableInputChannel
    {
        public event EventHandler<MessageIdEventArgs> ResponseMessageDelivered;
        public event EventHandler<MessageIdEventArgs> ResponseMessageNotDelivered;

        public void AttachReliableInputChannel(IReliableDuplexInputChannel reliableDuplexInputChannel)
        {
            using (EneterTrace.Entering())
            {
                lock (myDuplexInputChannelManipulatorLock)
                {
                    try
                    {
                        reliableDuplexInputChannel.ResponseMessageDelivered += OnResponseMessageDelivered;
                        reliableDuplexInputChannel.ResponseMessageNotDelivered += OnResponseMessageNotDelivered;

                        base.AttachDuplexInputChannel(reliableDuplexInputChannel);
                    }
                    catch (Exception err)
                    {
                        reliableDuplexInputChannel.ResponseMessageDelivered -= OnResponseMessageDelivered;
                        reliableDuplexInputChannel.ResponseMessageNotDelivered -= OnResponseMessageNotDelivered;

                        EneterTrace.Error(TracedObject + "failed to attach reliable duplex input channel.", err);
                        throw;
                    }
                }
            }
        }

        public void DetachReliableInputChannel()
        {
            using (EneterTrace.Entering())
            {
                lock (myDuplexInputChannelManipulatorLock)
                {
                    IReliableDuplexInputChannel aDetachedDuplexInputChannel = AttachedReliableInputChannel;

                    try
                    {
                        base.DetachDuplexInputChannel();
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + "failed to detach reliable duplex input channel.", err);
                    }

                    if (aDetachedDuplexInputChannel != null)
                    {
                        aDetachedDuplexInputChannel.ResponseMessageDelivered -= OnResponseMessageDelivered;
                        aDetachedDuplexInputChannel.ResponseMessageNotDelivered -= OnResponseMessageNotDelivered;
                    }
                }
            }
        }

        public bool IsReliableInputChannelAttached { get { return base.IsDuplexInputChannelAttached; } }

        public IReliableDuplexInputChannel AttachedReliableInputChannel
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    lock (myDuplexInputChannelManipulatorLock)
                    {
                        return base.AttachedDuplexInputChannel as IReliableDuplexInputChannel;
                    }
                }
            }
        }


        private void OnResponseMessageDelivered(object sender, MessageIdEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (ResponseMessageDelivered != null)
                {
                    try
                    {
                        ResponseMessageDelivered(this, e);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }

        private void OnResponseMessageNotDelivered(object sender, MessageIdEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (ResponseMessageNotDelivered != null)
                {
                    try
                    {
                        ResponseMessageNotDelivered(this, e);
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
