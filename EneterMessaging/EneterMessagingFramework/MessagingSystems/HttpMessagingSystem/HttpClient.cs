/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/


using System;
using System.IO;
using System.Net;
using System.Text;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.HttpMessagingSystem
{
    /// <summary>
    /// HTTP client.
    /// </summary>
    internal class HttpClient
    {
        public static HttpWebResponse Get(Uri uri)
        {
            using (EneterTrace.Entering())
            {
                HttpWebRequest aRequest = (HttpWebRequest)WebRequest.Create(uri);
                aRequest.Method = "GET";
                HttpWebResponse aResponse = aRequest.GetResponse() as HttpWebResponse;
                return aResponse;
            }
        }

        public static HttpWebResponse Post(Uri uri, string data)
        {
            using (EneterTrace.Entering())
            {
                byte[] aBytes = Encoding.UTF8.GetBytes(data);
                HttpWebResponse aResult = Post(uri, aBytes);
                return aResult;
            }
        }
        
        public static HttpWebResponse Post(Uri uri, byte[] data)
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

                HttpWebResponse aResponse = aRequest.GetResponse() as HttpWebResponse;

                return aResponse;
            }
        }
    }
}