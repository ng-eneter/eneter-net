/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if !SILVERLIGHT && !MONO

using System;
using System.IO;
using System.IO.Pipes;
using Eneter.Messaging.DataProcessing.Streaming;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.NamedPipeMessagingSystem
{
    internal class NamedPipeOutputChannel : IOutputChannel
    {
        public NamedPipeOutputChannel(string channelId, int timeOut)
        {
            using (EneterTrace.Entering())
            {
                if (string.IsNullOrEmpty(channelId))
                {
                    EneterTrace.Error(ErrorHandler.NullOrEmptyChannelId);
                    throw new ArgumentException(ErrorHandler.NullOrEmptyChannelId);
                }

                // Parse the string format to get the server name and the pipe name.
                Uri aPath = null;
                Uri.TryCreate(channelId, UriKind.Absolute, out aPath);
                if (aPath != null)
                {
                    myServerName = aPath.Host;

                    for (int i = 1; i < aPath.Segments.Length; ++i)
                    {
                        myPipeName += aPath.Segments[i].TrimEnd('/');
                    }
                }
                else
                {
                    string anError = TracedObject + ErrorHandler.InvalidUriAddress;
                    EneterTrace.Error(anError);
                    throw new ArgumentException(anError);
                }

                ChannelId = channelId;
                myTimeOut = timeOut;
            }
        }

        public string ChannelId { get; private set; }

        public void SendMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    byte[] aBufferedMessage = null;

                    using (MemoryStream aMemStream = new MemoryStream())
                    {
                        MessageStreamer.WriteMessage(aMemStream, message);
                        aBufferedMessage = aMemStream.ToArray();
                    }

                    using (NamedPipeClientStream aNamedPipeClientStream = new NamedPipeClientStream(myServerName, myPipeName, PipeDirection.Out))
                    {
                        aNamedPipeClientStream.Connect(myTimeOut);
                        aNamedPipeClientStream.Write(aBufferedMessage, 0, aBufferedMessage.Length);
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.SendMessageFailure, err);
                    throw;
                }
            }
        }

        private int myTimeOut;

        private string myServerName;
        private string myPipeName = "";


        private string TracedObject
        {
            get
            {
                return "The named pipe output channel '" + ChannelId + "' ";
            }
        }
    }
}

#endif