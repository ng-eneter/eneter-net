/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/


using System;
using System.Net;
using System.Threading;
using Eneter.Messaging.Diagnostic;


namespace Eneter.Messaging.MessagingSystems.HttpMessagingSystem
{
    internal class HttpListenerProvider
    {
        public HttpListenerProvider(string absoluteUri)
        {
            using (EneterTrace.Entering())
            {
                myAbsoluteUri = absoluteUri;
            }
        }

        public void StartListening(Action<HttpRequestContext> connectionHandler)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myListeningManipulatorLock))
                {
                    if (IsListening)
                    {
                        string aMessage = TracedObject + ErrorHandler.IsAlreadyListening;
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    if (connectionHandler == null)
                    {
                        throw new ArgumentNullException("The input parameter connectionHandler is null.");
                    }

                    try
                    {
                        // Set the listening flag so that the listening loop will work.
                        myStopListeningRequested = false;

                        myConnectionHandler = connectionHandler;

                        // Start listening for for incoming Http requests
                        myListener = new HttpListener();
                        myListener.Prefixes.Add(myAbsoluteUri);
                        myListener.Start();

                        // Do listening loop in another thread
                        myListeningThread = new Thread(DoListening);
                        myListeningThread.Start();
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.FailedToStartListening, err);

                        try
                        {
                            // Clear after failed start
                            StopListening();
                        }
                        catch
                        {
                            // We tried to clean after failure. The exception can be ignored.
                        }

                        throw;
                    }
                }
            }
        }

        public void StopListening()
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myListeningManipulatorLock))
                {
                    myStopListeningRequested = true;

                    if (myListener != null)
                    {
                        try
                        {
                            myListener.Stop();
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.IncorrectlyStoppedListening, err);
                        }

                        try
                        {
                            myListener.Close();
                        }
                        catch (Exception err)
                        {
                            EneterTrace.Warning(TracedObject + "failed to close the Http listener.", err);
                        }

                        myListener = null;
                    }

                    if (myListeningThread != null && myListeningThread.ThreadState != ThreadState.Unstarted)
                    {
                        if (!myListeningThread.Join(1000))
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.FailedToStopThreadId + myListeningThread.ManagedThreadId.ToString());

                            try
                            {
                                myListeningThread.Abort();
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.FailedToAbortThread, err);
                            }
                        }
                    }
                    myListeningThread = null;

                    myConnectionHandler = null;
                }
            }
        }

        public bool IsListening
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    using (ThreadLock.Lock(myListeningManipulatorLock))
                    {
                        return myListener != null;
                    }
                }
            }
        }

        /// <summary>
        /// Loop for the main listening thread.
        /// </summary>
        private void DoListening()
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    // Listening loop
                    while (!myStopListeningRequested)
                    {
                        // When the connection is established then handle it in another thread.
                        // Note: HandleConnection is the abstract method implemented in the derived class.
                        IAsyncResult anAsyncResult = myListener.BeginGetContext(ProcessConnection, myListener);

                        if (!myStopListeningRequested)
                        {
                            // Wait for the connection.
                            anAsyncResult.AsyncWaitHandle.WaitOne();
                            anAsyncResult.AsyncWaitHandle.Close();
                        }
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.FailedInListeningLoop, err);
                }
            }
        }

        private void ProcessConnection(IAsyncResult asyncResult)
        {
            using (EneterTrace.Entering())
            {
                HttpListener anHttpListener = (HttpListener)asyncResult.AsyncState;
                HttpListenerContext aListenerContext = null;
                try
                {
                    aListenerContext = anHttpListener.EndGetContext(asyncResult);

                    // Process the http request.
                    HttpRequestContext aClientContext = new HttpRequestContext(aListenerContext);
                    myConnectionHandler(aClientContext);
                }
                catch (ObjectDisposedException)
                {
                    // The listening was stopped.
                }
                catch (HttpListenerException err)
                {
                    // If the listening was stopped.
                    // The I/O operation has been aborted because of either a thread exit or an application request.
                    if (err.ErrorCode == 995)
                    {
                        // ignore
                    }
                    else
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.ProcessingHttpConnectionFailure, err);
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.ProcessingHttpConnectionFailure, err);
                }
                finally
                {
                    if (aListenerContext != null)
                    {
                        aListenerContext.Response.OutputStream.Close();
                        aListenerContext.Request.InputStream.Close();
                    }
                }
            }
        }


        private string myAbsoluteUri;
        private Action<HttpRequestContext> myConnectionHandler;

        private HttpListener myListener;
        private Thread myListeningThread;
        private volatile bool myStopListeningRequested;

        private object myListeningManipulatorLock = new object();


        private string TracedObject { get { return GetType().Name + " "; } }
    }
}