/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/


using System;
using System.IO;
using System.Net;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.HttpMessagingSystem
{
    /// <summary>
    /// Invokes the HTTP request.
    /// </summary>
    internal class HttpRequestInvoker
    {
        public static WebResponse InvokeGetRequest(Uri uri)
        {
            using (EneterTrace.Entering())
            {
                HttpWebRequest aRequest = (HttpWebRequest)WebRequest.Create(uri);
                WebResponse aResponse = aRequest.GetResponse();

                return aResponse;
            }
        }

        public static WebResponse InvokePostRequest(Uri uri, byte[] data)
        {
            using (EneterTrace.Entering())
            {
                HttpWebRequest aRequest = (HttpWebRequest)WebRequest.Create(uri);
                aRequest.Method = "POST";

                Stream aRequestStream = aRequest.GetRequestStream();

                // Send the message.
                // Note: The message is sent by calling GetResponse().
                aRequestStream.Write(data, 0, data.Length);

                // Note: The communication in Compact Framework does not work
                //       if the stream is not closed.
                aRequestStream.Close();

                WebResponse aResponse = aRequest.GetResponse();

                return aResponse;
            }
        }
    }
}