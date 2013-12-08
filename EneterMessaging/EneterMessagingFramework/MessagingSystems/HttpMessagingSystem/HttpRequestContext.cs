/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/

#if !COMPACT_FRAMEWORK && !SILVERLIGHT

using System.Net;
using Eneter.Messaging.DataProcessing.Streaming;
using Eneter.Messaging.Diagnostic;
using System;

namespace Eneter.Messaging.MessagingSystems.HttpMessagingSystem
{
    internal class HttpRequestContext
    {
        public HttpRequestContext(HttpListenerContext httpListenerContext)
        {
            using (EneterTrace.Entering())
            {
                myHttpListenerContext = httpListenerContext;
            }
        }

        public string HttpMethod { get { return myHttpListenerContext.Request.HttpMethod.ToUpper(); } }

        public Uri Uri { get { return myHttpListenerContext.Request.Url; } }

        public IPEndPoint RemoteEndPoint { get { return myHttpListenerContext.Request.RemoteEndPoint; } }

        /// <summary>
        /// Returns the body content of the HTTP request.
        /// </summary>
        /// <returns></returns>
        public byte[] GetRequestMessage()
        {
            using (EneterTrace.Entering())
            {
                byte[] aRequestMessage = StreamUtil.ReadBytes(myHttpListenerContext.Request.InputStream, (int)myHttpListenerContext.Request.ContentLength64);
                return aRequestMessage;
            }
        }

        /// <summary>
        /// Responses back to the HTTP client.
        /// </summary>
        /// <param name="message">body content.</param>
        public void Response(byte[] message)
        {
            using (EneterTrace.Entering())
            {
                myHttpListenerContext.Response.OutputStream.Write(message, 0, message.Length);
            }
        }

        /// <summary>
        /// Responses back the error.
        /// </summary>
        /// <param name="statusCode"></param>
        public void ResponseError(int statusCode)
        {
            using (EneterTrace.Entering())
            {
                myHttpListenerContext.Response.StatusCode = statusCode;
            }
        }

        private HttpListenerContext myHttpListenerContext;
    }
}


#endif