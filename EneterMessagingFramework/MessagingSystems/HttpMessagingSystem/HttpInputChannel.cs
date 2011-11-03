/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/


#if !SILVERLIGHT

using System;
using System.IO;
using System.Net;
using Eneter.Messaging.DataProcessing.Streaming;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.HttpMessagingSystem
{
    internal class HttpInputChannel : HttpInputChannelBase, IInputChannel
    {
        public event EventHandler<ChannelMessageEventArgs> MessageReceived;

        public HttpInputChannel(string channelId)
            : base(channelId)
        {
            using (EneterTrace.Entering())
            {
            }
        }

        /// <summary>
        /// Processes the Http requests.
        /// It gets the message and puts it to the queue. The message is then removed from the queue by
        /// the working thread that notifies the subscriber.
        /// </summary>
        /// <param name="asyncResult"></param>
        protected override void HandleConnection(IAsyncResult asyncResult)
        {
            using (EneterTrace.Entering())
            {
                HttpListener anHttpListener = (HttpListener)asyncResult.AsyncState;

                try
                {
                    HttpListenerContext aListenerContext = anHttpListener.EndGetContext(asyncResult);

                    try
                    {
                        if (!myStopHttpListeningRequested)
                        {
                            Stream anInputStream = aListenerContext.Request.InputStream;

                            // First read the message to the buffer.
                            object aMessage = null;
                            using (MemoryStream aMemStream = new MemoryStream())
                            {
                                int aSize = 0;
                                byte[] aBuffer = new byte[32768];
                                while ((aSize = anInputStream.Read(aBuffer, 0, aBuffer.Length)) != 0)
                                {
                                    aMemStream.Write(aBuffer, 0, aSize);
                                }

                                // Read the message from the buffer.
                                aMemStream.Position = 0;
                                aMessage = MessageStreamer.ReadMessage(aMemStream);
                            }

                            if (!myStopHttpListeningRequested && aMessage != null)
                            {
                                // Put the message to the queue from where the working thread removes it to notify
                                // subscribers of the input channel.
                                // Note: therfore subscribers of the input channel are notified allways in one thread.
                                myMessageProcessingThread.EnqueueMessage(aMessage);
                            }
                        }
                    }
                    finally
                    {
                        aListenerContext.Response.OutputStream.Close();
                        aListenerContext.Request.InputStream.Close();
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.ProcessingHttpConnectionFailure, err);
                }
            }
        }


        /// <summary>
        /// The method is called from the working thread when a message shall be processed.
        /// Messages comming from from diffrent receiving threads are put to the queue where the working
        /// thread removes them one by one and notify the subscribers on the input channel.
        /// Therefore the channel notifies always in one thread.
        /// </summary>
        protected override void MessageHandler(object message)
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


        protected override string TracedObject
        {
            get 
            {
                string aChannelId = (ChannelId != null) ? ChannelId : "";
                return "Http input channel '" + aChannelId + "' "; 
            }
        }

    }
}

#endif