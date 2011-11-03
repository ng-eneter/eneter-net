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
    internal class HttpOutputChannel : IOutputChannel
    {
        public HttpOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                if (string.IsNullOrEmpty(channelId))
                {
                    EneterTrace.Error(ErrorHandler.NullOrEmptyChannelId);
                    throw new ArgumentException(ErrorHandler.NullOrEmptyChannelId);
                }

                try
                {
                    myUriBuilder = new UriBuilder(channelId);
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.InvalidUriAddress, err);
                    throw;
                }

                ChannelId = channelId;
                myUriBuilder = new UriBuilder(channelId);
            }
        }

        public string ChannelId { get; private set; }

        public void SendMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    using (WebClient aWebClient = new WebClient())
                    {
                        using (MemoryStream aMemoryStream = new MemoryStream())
                        {
                            MessageStreamer.WriteMessage(aMemoryStream, message);
                            aWebClient.UploadData(myUriBuilder.Uri, aMemoryStream.ToArray());
                        }
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.SendMessageFailure, err);
                    throw;
                }
            }
        }

        private UriBuilder myUriBuilder;

        private string TracedObject
        {
            get 
            {
                return "The Http output channel '" + ChannelId + "' "; 
            }
        }
    }
}

#endif
