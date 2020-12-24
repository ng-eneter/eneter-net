

using System;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.Infrastructure.Attachable
{
    internal abstract class AttachableDuplexOutputChannelBase : IAttachableDuplexOutputChannel
    {
        public virtual void AttachDuplexOutputChannel(IDuplexOutputChannel duplexOutputChannel)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myDuplexOutputChannelManipulatorLock))
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

                        string aMessage = TracedObject + ErrorHandler.FailedToOpenConnection;
                        EneterTrace.Error(aMessage, err);
                        throw;
                    }
                }
            }
        }

        public virtual void DetachDuplexOutputChannel()
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myDuplexOutputChannelManipulatorLock))
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
                            AttachedDuplexOutputChannel.ConnectionOpened -= OnConnectionOpened;
                            AttachedDuplexOutputChannel.ConnectionClosed -= OnConnectionClosed;
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
                    using (ThreadLock.Lock(myDuplexOutputChannelManipulatorLock))
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
                using (ThreadLock.Lock(myDuplexOutputChannelManipulatorLock))
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
                    AttachedDuplexOutputChannel.ConnectionOpened += OnConnectionOpened;
                    AttachedDuplexOutputChannel.ConnectionClosed += OnConnectionClosed;
                    AttachedDuplexOutputChannel.ResponseMessageReceived += OnResponseMessageReceived;
                }
            }
        }

        protected abstract void OnConnectionOpened(object sender, DuplexChannelEventArgs e);

        protected abstract void OnConnectionClosed(object sender, DuplexChannelEventArgs e);

        protected abstract void OnResponseMessageReceived(object sender, DuplexChannelMessageEventArgs e);


        protected object myDuplexOutputChannelManipulatorLock = new object();
        public IDuplexOutputChannel AttachedDuplexOutputChannel { get; private set; }


        protected abstract string TracedObject { get; }
    }
}
