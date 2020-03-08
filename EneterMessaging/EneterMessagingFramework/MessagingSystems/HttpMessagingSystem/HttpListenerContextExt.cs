/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright (c) Ondrej Uzovic 2020
*/

using Eneter.Messaging.DataProcessing.Streaming;
using Eneter.Messaging.Diagnostic;
using System.Net;
using System.Text;

namespace Eneter.Messaging.MessagingSystems.HttpMessagingSystem
{
    /// <summary>
    /// Extends HttpListenerContext by helper methods.
    /// </summary>
    internal static class HttpListenerContextExt
    {
        /// <summary>
        /// Gets all request message data in string.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string GetRequestMessageStr(this HttpListenerContext context)
        {
            using (EneterTrace.Entering())
            {
                byte[] aRequestMessage = context.GetRequestMessage();
                Encoding aEncoding = context.Request.ContentEncoding != null ? context.Request.ContentEncoding : Encoding.UTF8;
                string aResult = aEncoding.GetString(aRequestMessage);
                return aResult;
            }
        }

        /// <summary>
        /// Gets the all request message data in bytes.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static byte[] GetRequestMessage(this HttpListenerContext context)
        {
            using (EneterTrace.Entering())
            {
                byte[] aRequestMessage = StreamUtil.ReadBytes(context.Request.InputStream, (int)context.Request.ContentLength64);
                return aRequestMessage;
            }
        }

        /// <summary>
        /// Sends the response message.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        public static void SendResponseMessage(this HttpListenerContext context, byte[] message)
        {
            using (EneterTrace.Entering())
            {
                context.Response.OutputStream.Write(message, 0, message.Length);
            }
        }

        /// <summary>
        /// Sends the response message.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="message"></param>
        public static void SendResponseMessage(this HttpListenerContext context, string message)
        {
            using (EneterTrace.Entering())
            {
                if (context.Response.ContentEncoding == null)
                {
                    context.Response.ContentEncoding = Encoding.UTF8;
                }

                byte[] aBytes = context.Response.ContentEncoding.GetBytes(message);
                context.Response.OutputStream.Write(aBytes, 0, message.Length);
            }
        }
    }
}
