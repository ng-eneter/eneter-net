

using System;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.Infrastructure.Attachable
{
    internal abstract class AttachableDuplexInputChannelBase : IAttachableDuplexInputChannel
    {
        public virtual void AttachDuplexInputChannel(IDuplexInputChannel duplexInputChannel)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myDuplexInputChannelManipulatorLock))
                {
                    Attach(duplexInputChannel);

                    try
                    {
                        AttachedDuplexInputChannel.StartListening();
                    }
                    catch (Exception err)
                    {
                        try
                        {
                            DetachDuplexInputChannel();
                        }
                        catch
                        {
                            // Ignore exception in exception.
                        }

                        EneterTrace.Error(TracedObject + ErrorHandler.FailedToStartListening, err);
                        throw;
                    }
                }
            }
        }

        public virtual void DetachDuplexInputChannel()
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myDuplexInputChannelManipulatorLock))
                {
                    if (AttachedDuplexInputChannel != null)
                    {
                        try
                        {
                            AttachedDuplexInputChannel.StopListening();
                        }
                        finally
                        {
                            AttachedDuplexInputChannel.MessageReceived -= OnRequestMessageReceived;
                            AttachedDuplexInputChannel.ResponseReceiverConnected -= OnResponseReceiverConnected;
                            AttachedDuplexInputChannel.ResponseReceiverDisconnected -= OnResponseReceiverDisconnected;
                            AttachedDuplexInputChannel = null;
                        }
                    }
                }
            }
        }

        public virtual bool IsDuplexInputChannelAttached
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    using (ThreadLock.Lock(myDuplexInputChannelManipulatorLock))
                    {
                        return AttachedDuplexInputChannel != null;
                    }
                }
            }
        }


        private void Attach(IDuplexInputChannel duplexInputChannel)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myDuplexInputChannelManipulatorLock))
                {
                    if (duplexInputChannel == null)
                    {
                        string aMessage = TracedObject + "failed to attach duplex input channel because the input parameter 'duplexInputChannel' is null.";
                        EneterTrace.Error(aMessage);
                        throw new ArgumentNullException(aMessage);
                    }

                    if (string.IsNullOrEmpty(duplexInputChannel.ChannelId))
                    {
                        string aMessage = TracedObject + "failed to attach duplex input channel because the input parameter 'duplexInputChannel' has empty or null channel id.";
                        EneterTrace.Error(aMessage);
                        throw new ArgumentException(aMessage);
                    }

                    if (IsDuplexInputChannelAttached)
                    {
                        string aMessage = TracedObject + "failed to attach duplex input channel '" + duplexInputChannel.ChannelId + "' because the channel is already attached.";
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    AttachedDuplexInputChannel = duplexInputChannel;

                    AttachedDuplexInputChannel.MessageReceived += OnRequestMessageReceived;
                    AttachedDuplexInputChannel.ResponseReceiverConnected += OnResponseReceiverConnected;
                    AttachedDuplexInputChannel.ResponseReceiverDisconnected += OnResponseReceiverDisconnected;
                }
            }
        }


        protected abstract void OnRequestMessageReceived(object sender, DuplexChannelMessageEventArgs e);

        protected abstract void OnResponseReceiverConnected(object sender, ResponseReceiverEventArgs e);

        protected abstract void OnResponseReceiverDisconnected(object sender, ResponseReceiverEventArgs e);


        public IDuplexInputChannel AttachedDuplexInputChannel { get; private set; }

        protected object myDuplexInputChannelManipulatorLock = new object();

        protected abstract string TracedObject { get; } 
    }
}
