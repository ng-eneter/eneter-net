/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if !SILVERLIGHT

using System;
using System.IO;
using Eneter.Messaging.DataProcessing.Streaming;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.Infrastructure.Attachable;

namespace Eneter.Messaging.Nodes.Bridge
{
    internal class Bridge : AttachableOutputChannelBase, IBridge
    {
        public void SendMessage(Stream message)
        {
            using (EneterTrace.Entering())
            {
                if (!IsOutputChannelAttached)
                {
                    string anError = TracedObject + "failed to send the message because it is not attached to an output channel.";
                    throw new InvalidOperationException(anError);
                }

                try
                {
                    object aMessage = null;

                    // If the message is not in the memory stream then
                    // because of performance read it first to the memory stream.
                    if (message is MemoryStream == false)
                    {
                        using (MemoryStream aMemoryStream = new MemoryStream())
                        {
                            int aSize = 0;
                            byte[] aBuffer = new byte[32768];
                            while ((aSize = message.Read(aBuffer, 0, aBuffer.Length)) != 0)
                            {
                                aMemoryStream.Write(aBuffer, 0, aSize);
                            }

                            aMemoryStream.Position = 0;
                            aMessage = MessageStreamer.ReadMessage(aMemoryStream);
                        }
                    }
                    else
                    {
                        MemoryStream aMemoryStream = (MemoryStream)message;
                        aMessage = MessageStreamer.ReadMessage(aMemoryStream);
                    }

                    AttachedOutputChannel.SendMessage(aMessage);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.SendMessageFailure, err);
                    throw;
                }
            }
        }


        private string TracedObject
        {
            get
            {
                string aChannelId = (AttachedOutputChannel != null) ? AttachedOutputChannel.ChannelId : "";
                return "The Bridge attached to the output channel '" + aChannelId + "' ";
            }
        }
        
    }
}

#endif