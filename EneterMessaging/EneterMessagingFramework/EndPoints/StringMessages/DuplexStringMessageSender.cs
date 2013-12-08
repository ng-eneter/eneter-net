/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Infrastructure.Attachable;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.EndPoints.StringMessages
{
    internal class DuplexStringMessageSender : AttachableDuplexOutputChannelBase, IDuplexStringMessageSender
    {
        public event EventHandler<DuplexChannelEventArgs> ConnectionOpened;
        public event EventHandler<DuplexChannelEventArgs> ConnectionClosed;
        public event EventHandler<StringResponseReceivedEventArgs> ResponseReceived;

        public override void AttachDuplexOutputChannel(IDuplexOutputChannel duplexOutputChannel)
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    try
                    {
                        duplexOutputChannel.ConnectionOpened += OnConnectionOpened;
                        duplexOutputChannel.ConnectionClosed += OnConnectionClosed;
                        base.AttachDuplexOutputChannel(duplexOutputChannel);
                    }
                    catch
                    {
                        try
                        {
                            DetachDuplexOutputChannel();
                        }
                        catch
                        {
                        }

                        throw;
                    }
                }
            }
        }

        public override void DetachDuplexOutputChannel()
        {
            using (EneterTrace.Entering())
            {
                lock (myConnectionManipulatorLock)
                {
                    IDuplexOutputChannel anAttachedDuplexOutputChannel = AttachedDuplexOutputChannel;

                    base.DetachDuplexOutputChannel();

                    if (anAttachedDuplexOutputChannel != null)
                    {
                        anAttachedDuplexOutputChannel.ConnectionOpened -= OnConnectionOpened;
                        anAttachedDuplexOutputChannel.ConnectionClosed -= OnConnectionClosed;
                    }
                }
            }
        }

        public void SendMessage(string message)
        {
            using (EneterTrace.Entering())
            {
                if (AttachedDuplexOutputChannel == null)
                {
                    string anError = TracedObject + ErrorHandler.ChannelNotAttached;
                    EneterTrace.Error(anError);
                    throw new InvalidOperationException(anError);
                }

                try
                {
                    AttachedDuplexOutputChannel.SendMessage(message);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.SendMessageFailure, err);
                    throw;
                }
            }
        }

        protected override void OnResponseMessageReceived(object sender, DuplexChannelMessageEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (ResponseReceived == null)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.NobodySubscribedForMessage);
                    return;
                }

                if (e.Message is string == false)
                {
                    string anErrorMessage = TracedObject + "failed to receive the response message because the message is not string.";
                    EneterTrace.Error(anErrorMessage);
                    return;
                }

                try
                {
                    ResponseReceived(this, new StringResponseReceivedEventArgs((string)e.Message));
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                }
            }
        }

        private void OnConnectionOpened(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                Notify(ConnectionOpened, e);
            }
        }

        private void OnConnectionClosed(object sender, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                Notify(ConnectionClosed, e);
            }
        }

        private void Notify(EventHandler<DuplexChannelEventArgs> handler, DuplexChannelEventArgs e)
        {
            using (EneterTrace.Entering())
            {
                if (handler != null)
                {
                    try
                    {
                        handler(this, e);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
            }
        }

        private object myConnectionManipulatorLock = new object();

        protected override string TracedObject
        {
            get
            {
                return GetType().Name + " ";
            }
        }
    }
}
