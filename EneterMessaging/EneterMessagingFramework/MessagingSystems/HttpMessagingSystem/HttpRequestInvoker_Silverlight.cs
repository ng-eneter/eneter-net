/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

#if SILVERLIGHT

using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.HttpMessagingSystem
{
    internal static class HttpRequestInvoker
    {
        /// <summary>
        /// This method is workaround for the Silverlight bug.
        /// The problem is, that when the asynchronous HttpRequest is invoked, it is not possible to wait
        /// in the Silverlight thread until it is completed. --> It hangs.
        /// </summary>
        public static void InvokeNotInSilverlightThread(Action action)
        {
            using (EneterTrace.Entering())
            {
                // If it is the Silverlight thread, then invoke it in another thread.
                if (Deployment.Current.Dispatcher.CheckAccess())
                {
                    ThreadPool.QueueUserWorkItem(x => action());
                }
                else
                {
                    action();
                }
            }
        }

        public static WebResponse InvokeGetRequest(Uri uri)
        {
            using (EneterTrace.Entering())
            {
                // Note: There is a known problem with caching of GET requests in Silverlight.
                //       To overcome it it is needed to differentiate between requests so that they are understood
                //       as different requests and so they are not chached.
                int anId = Interlocked.Increment(ref myRequestId);
                string aModifiedQuery = string.Concat(uri.Query, '&', anId.ToString());
                Uri aModifiedUri = new Uri(uri, aModifiedQuery);
                HttpWebRequest aWebRequest = (HttpWebRequest)WebRequest.Create(aModifiedUri);
                aWebRequest.Method = "GET";

                HttpWebResponse aWebResponse = null;
                Exception aDetectedError = null;
                AutoResetEvent aSendingCompleted = new AutoResetEvent(false);

                AutoResetEvent aResponseCompletedSignal = new AutoResetEvent(false);
                aWebRequest.BeginGetResponse(x =>
                    {
                        using (EneterTrace.Entering())
                        {
                            try
                            {
                                aWebResponse = (HttpWebResponse)aWebRequest.EndGetResponse(x);
                            }
                            catch (Exception err)
                            {
                                aDetectedError = err;
                            }

                            aResponseCompletedSignal.Set();
                        }
                    }, null);

                if (!aResponseCompletedSignal.WaitOne(30000))
                {
                    aDetectedError = new TimeoutException("The sending of the http request exceeded 30 seconds timeout.");
                }

                if (aDetectedError != null)
                {
                    throw aDetectedError;
                }

                return aWebResponse;
            }
        }

        public static WebResponse InvokePostRequest(Uri uri, byte[] encodedMessage)
        {
            using (EneterTrace.Entering())
            {
                WebRequest aWebRequest = WebRequest.Create(uri);
                aWebRequest.Method = "POST";

                WebResponse aWebResponse = null;
                Exception aDetectedError = null;
                AutoResetEvent aSendingCompleted = new AutoResetEvent(false);

                // Get the stream to send the message.
                aWebRequest.BeginGetRequestStream(x =>
                    {
                        using (EneterTrace.Entering())
                        {
                            try
                            {
                                using (Stream aStream = aWebRequest.EndGetRequestStream(x))
                                {
                                    // Send the request by writing to the stream.
                                    aStream.Write(encodedMessage, 0, encodedMessage.Length);
                                }

                                // Get the response.
                                AutoResetEvent aResponseCompletedEvent = new AutoResetEvent(false);
                                aWebRequest.BeginGetResponse(xx =>
                                    {
                                        try
                                        {
                                            // If this call throws an exception, then there is a connection problem.
                                            aWebResponse = (HttpWebResponse)aWebRequest.EndGetResponse(xx);
                                        }
                                        catch (Exception err)
                                        {
                                            aDetectedError = err;
                                        }

                                        // Indicate, the getting of the response is finished.
                                        aResponseCompletedEvent.Set();

                                    }, null);

                                // Wait until the response is processed.
                                if (!aResponseCompletedEvent.WaitOne(30000))
                                {
                                    aDetectedError = new TimeoutException("The waiting for the http response exceeded 30 seconds timeout.");
                                    aResponseCompletedEvent.Set();
                                }
                            }
                            catch (Exception err)
                            {
                                aDetectedError = err;
                            }

                            // Indicate, the sending of the request is completed.
                            aSendingCompleted.Set();
                        }
                    }, null);

                // Wait until the sending is completed.
                // Note: The waiting time must be higher as the waiting in the above delegate.
                if (!aSendingCompleted.WaitOne(31000))
                {
                    aDetectedError = new TimeoutException("The sending of the http request exceeded 30 seconds timeout.");
                }

                if (aDetectedError != null)
                {
                    throw aDetectedError;
                }

                return aWebResponse;
            }
        }


        private static int myRequestId;
    }
}

#endif