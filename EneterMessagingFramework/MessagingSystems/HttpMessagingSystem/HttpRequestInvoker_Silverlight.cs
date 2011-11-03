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
using Eneter.Messaging.DataProcessing.Streaming;
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
        /// <param name="anAddress"></param>
        /// <param name="request"></param>
        public static void InvokeNotInSilverlightThread(Uri anAddress, object request)
        {
            using (EneterTrace.Entering())
            {
                // If if is the Silverlight thread, then invoke it in another thread.
                if (Deployment.Current.Dispatcher.CheckAccess())
                {
                    EneterTrace.Debug("This is the Silverlight thread. So we invoke the HttpRequest in different thread.");
                    WaitCallback aSendRequest = x => HttpRequestInvoker.InvokeWebRequest(anAddress, request);
                    ThreadPool.QueueUserWorkItem(aSendRequest);
                }
                else
                {
                    HttpRequestInvoker.InvokeWebRequest(anAddress, request);
                }
            }
        }

        public static WebResponse InvokeWebRequest(Uri anAddress, object request)
        {
            using (EneterTrace.Entering())
            {
                WebRequest aWebRequest = WebRequest.Create(anAddress);
                aWebRequest.Method = "POST";

                WebResponse aWebResponse = null;
                Exception aDetectedError = null;
                AutoResetEvent aSendingCompleted = new AutoResetEvent(false);

                // Get the stream to send the polling request.
                aWebRequest.BeginGetRequestStream(x =>
                    {
                        using (EneterTrace.Entering())
                        {
                            try
                            {
                                using (Stream aStream = aWebRequest.EndGetRequestStream(x))
                                {
                                    // Send the request by writing to the stream.
                                    MessageStreamer.WriteMessage(aStream, request);
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
    }
}

#endif