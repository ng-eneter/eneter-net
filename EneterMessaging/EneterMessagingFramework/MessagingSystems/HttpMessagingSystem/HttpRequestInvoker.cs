/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/

#if !SILVERLIGHT

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
#if COMPACT_FRAMEWORK
            	using (ThreadLock.Lock(myLock)
#endif
                {
                    HttpWebRequest aRequest = (HttpWebRequest)WebRequest.Create(uri);
#if COMPACT_FRAMEWORK
                    aRequest.AllowWriteStreamBuffering = true;
#endif
                    WebResponse aResponse = aRequest.GetResponse();

                    return aResponse;
                }
            }
        }

        public static WebResponse InvokePostRequest(Uri uri, byte[] data)
        {
            using (EneterTrace.Entering())
            {
#if COMPACT_FRAMEWORK
            	using (ThreadLock.Lock(myLock)
#endif
            	{
                    HttpWebRequest aRequest = (HttpWebRequest)WebRequest.Create(uri);
                    aRequest.Method = "POST";

#if COMPACT_FRAMEWORK
                    aRequest.AllowWriteStreamBuffering = true;
#endif

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
        
#if COMPACT_FRAMEWORK
        private static object myLock = new object();
#endif
    }
}


#endif