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
    public static class HttpWebClient
    {
        /// <summary>
        /// Sends the GET request to the HTTP server.
        /// </summary>
        /// <param name="uri">The address including the input parameters.</param>
        /// <returns>Response from the HTTP server.</returns>
        public static HttpWebResponse Get(Uri uri)
        {
            using (EneterTrace.Entering())
            {
                HttpWebResponse aResponse = Request("GET", uri, (byte[])null);
                return aResponse;
            }
        }

        /// <summary>
        /// Sends the POST request to the HTTP server.
        /// </summary>
        /// <param name="uri">The address including the input parameters.</param>
        /// <param name="body">Data for the post request.</param>
        /// <returns>Response from the HTTP server.</returns>
        public static HttpWebResponse Post(Uri uri, string body)
        {
            using (EneterTrace.Entering())
            {
                HttpWebResponse aResult = Request("POST", uri, body);
                return aResult;
            }
        }

        /// <summary>
        /// Sends the POST request to the HTTP server.
        /// </summary>
        /// <param name="uri">The address including the input parameters.</param>
        /// <param name="body">Data for the post request.</param>
        /// <returns>Response from the HTTP server.</returns>
        public static HttpWebResponse Post(Uri uri, byte[] body)
        {
            using (EneterTrace.Entering())
            {
                HttpWebResponse aResponse = Request("POST", uri, body);
                return aResponse;
            }
        }

        /// <summary>
        /// Sends the PUT request to the HTTP server.
        /// </summary>
        /// <param name="uri">The address including the input parameters.</param>
        /// <param name="body">Data for the put request.</param>
        /// <returns>Response from the HTTP server.</returns>
        public static HttpWebResponse Put(Uri uri, string body)
        {
            using (EneterTrace.Entering())
            {
                HttpWebResponse aResponse = Request("PUT", uri, body);
                return aResponse;
            }
        }

        /// <summary>
        /// Sends the PUT request to the HTTP server.
        /// </summary>
        /// <param name="uri">The address including the input parameters.</param>
        /// <param name="body">Data for the put request.</param>
        /// <returns>Response from the HTTP server.</returns>
        public static HttpWebResponse Put(Uri uri, byte[] body)
        {
            using (EneterTrace.Entering())
            {
                HttpWebResponse aResponse = Request("PUT", uri, body);
                return aResponse;
            }
        }

        /// <summary>
        /// Sends the PATCH request to the HTTP server.
        /// </summary>
        /// <param name="uri">The address including the input parameters.</param>
        /// <param name="body">Data for the patch request.</param>
        /// <returns>Response from the HTTP server.</returns>
        public static HttpWebResponse Patch(Uri uri, string body)
        {
            using (EneterTrace.Entering())
            {
                HttpWebResponse aResponse = Request("PATCH", uri, body);
                return aResponse;
            }
        }

        /// <summary>
        /// Sends the PATCH request to the HTTP server.
        /// </summary>
        /// <param name="uri">The address including the input parameters.</param>
        /// <param name="body">Data for the patch request.</param>
        /// <returns>Response from the HTTP server.</returns>
        public static HttpWebResponse Patch(Uri uri, byte[] body)
        {
            using (EneterTrace.Entering())
            {
                HttpWebResponse aResponse = Request("PATCH", uri, body);
                return aResponse;
            }
        }

        /// <summary>
        /// Sends the DELETE request to the HTTP server.
        /// </summary>
        /// <param name="uri">The address including the input parameters.</param>
        /// <returns>Response from the HTTP server.</returns>
        public static HttpWebResponse Delete(Uri uri)
        {
            using (EneterTrace.Entering())
            {
                HttpWebResponse aResponse = Request("DELETE", uri, (byte[])null);
                return aResponse;
            }
        }

        /// <summary>
        /// Sends a request to the HTTP server.
        /// </summary>
        /// <param name="httpMethod">HTTP method specifying the request.</param>
        /// <param name="uri">The address including the input parameters.</param>
        /// <param name="body">The request data.</param>
        /// <returns>Response from the HTTP server.</returns>
        public static HttpWebResponse Request(string httpMethod, Uri uri, string body)
        {
            using (EneterTrace.Entering())
            {
                byte[] aBytes = body != null ? Encoding.UTF8.GetBytes(body) : null;
                HttpWebResponse aResult = Request(httpMethod, uri, aBytes);
                return aResult;
            }
        }

        /// <summary>
        /// Sends a request to the HTTP server.
        /// </summary>
        /// <param name="httpMethod">HTTP method specifying the request.</param>
        /// <param name="uri">The address including the input parameters.</param>
        /// <param name="body">The request data.</param>
        /// <returns>Response from the HTTP server.</returns>
        public static HttpWebResponse Request(string httpMethod, Uri uri, byte[] body)
        {
            using (EneterTrace.Entering())
            {
                HttpWebRequest aRequest = (HttpWebRequest)WebRequest.Create(uri);
                aRequest.Method = httpMethod;

                if (body != null)
                {
                    using (Stream aRequestStream = aRequest.GetRequestStream())
                    {
                        aRequestStream.Write(body, 0, body.Length);
                    }
                }

                // Sends the message.
                HttpWebResponse aResponse = aRequest.GetResponse() as HttpWebResponse;

                return aResponse;
            }
        }
    }
}