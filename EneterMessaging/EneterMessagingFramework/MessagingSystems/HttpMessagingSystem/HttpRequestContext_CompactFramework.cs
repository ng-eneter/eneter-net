/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/

#if COMPACT_FRAMEWORK

using System;
using System.IO;
using System.Net;

using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.HttpMessagingSystem
{
    internal class HttpRequestContext
    {
        public HttpRequestContext(Uri uri, string httpMethod, IPEndPoint remoteEndPoint, byte[] requestMessage, Stream responseStream)
        {
            using (EneterTrace.Entering())
            {
                Uri = uri;
                HttpMethod = httpMethod;
                RemoteEndPoint = remoteEndPoint;
                myResponseStream = responseStream;
                myRequestMessage = requestMessage;
            }
        }

        public Uri Uri { get; private set; }
        
        public string HttpMethod { get; private set; }
        
        public IPEndPoint RemoteEndPoint { get; private set; }

        /// <summary>
        /// Returns the body content of the HTTP request.
        /// </summary>
        /// <returns></returns>
        public byte[] GetRequestMessage()
        {
            return myRequestMessage;
        }

        /// <summary>
        /// Responses back to the HTTP client.
        /// </summary>
        /// <param name="message">body content.</param>
        public void Response(byte[] message)
        {
            using (EneterTrace.Entering())
            {
                if (IsResponded)
                {
                    throw new InvalidOperationException("It is not allowed to send more than one response message per request message.");
                }

                // Encode the http response.
                byte[] aResponse = HttpFormatter.EncodeResponse(200, message, false);

                // Send the message.
                myResponseStream.Write(aResponse, 0, aResponse.Length);

                IsResponded = true;
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
                if (IsResponded)
                {
                    throw new InvalidOperationException("It is not allowed to send more than one response message per request message.");
                }

                // Encode the http response.
                byte[] aResponse = HttpFormatter.EncodeResponse(statusCode, null, false);

                // Send the message.
                myResponseStream.Write(aResponse, 0, aResponse.Length);

                IsResponded = true;
            }
        }

        // Returns true if the response has already been sent.
        public bool IsResponded { get; private set; }

        private Stream myResponseStream;
        private byte[] myRequestMessage;
    }
}

#endif