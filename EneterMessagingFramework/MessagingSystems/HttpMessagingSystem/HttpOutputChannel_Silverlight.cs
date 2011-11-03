/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if SILVERLIGHT

using System;
using System.Net;
using System.Net.Browser;
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

                // Specify, that the http/https requests connected to this output duplex channel will be handled by the client and not by the browser.
                // Note: If the requests are handled by the Browser, then the browser would "blink" the text message in its statusbar, that
                //       is connecting, retrieving data ... regularly for every poll request.
                try
                {
                    if (WebRequest.RegisterPrefix(myUriBuilder.Uri.AbsoluteUri, WebRequestCreator.ClientHttp) == false)
                    {
                        EneterTrace.Warning(TracedObject + "failed to register, that the web request " + myUriBuilder.Uri.OriginalString + " shall be handled by the client and not by the browser.");
                    }
                }
                catch (Exception err)
                {
                    // In case of the error, the requests will be handled by the Browser with the comsmetic side-efect - the user will
                    // see the communication status messages in the status bar of the browser.
                    EneterTrace.Warning(TracedObject + "failed to register, that the web request " + myUriBuilder.Uri.OriginalString + " shall be handled by the client and not by the browser.", err);
                }

                ChannelId = channelId;
            }
        }

        public string ChannelId { get; private set; }

        public void SendMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    HttpRequestInvoker.InvokeNotInSilverlightThread(myUriBuilder.Uri, message);
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