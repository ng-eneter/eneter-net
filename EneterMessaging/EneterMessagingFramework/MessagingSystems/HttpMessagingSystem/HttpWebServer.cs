/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright (c) Ondrej Uzovic 2012
*/


using System;
using System.Net;
using System.Threading;
using Eneter.Messaging.Diagnostic;


namespace Eneter.Messaging.MessagingSystems.HttpMessagingSystem
{
    /// <summary>
    /// HTTP server.
    /// </summary>
    internal class HttpServer
    {
        /// <summary>
        /// Constructs the HTTP server.
        /// </summary>
        /// <param name="absoluteUri">The root HTTP address to be listened.</param>
        public HttpServer(string absoluteUri)
        {
            using (EneterTrace.Entering())
            {
                myAbsoluteUri = absoluteUri;
            }
        }

        /// <summary>
        /// Allows to set the custom factory method for the underlying HttpListner.
        /// </summary>
        /// <remarks>
        /// If it is null then HttpListener is created by a default way.
        /// </remarks>
        public Func<HttpListener> HttpListenerFactory { get; set; }

        /// <summary>
        /// Starts the listening.
        /// </summary>
        /// <param name="connectionHandler">Callback to be called when a request is received.</param>
        public void StartListening(Action<HttpListenerContext> connectionHandler)
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

                        // Start listening for incoming Http requests
                        myListener = HttpListenerFactory != null ? HttpListenerFactory() : CreateHttpListener();
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

        /// <summary>
        /// Stops the listening.
        /// </summary>
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

        /// <summary>
        /// Returns true if the listening is active.
        /// </summary>
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
                    myConnectionHandler(aListenerContext);
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

        /// <summary>
        /// Default factory method for HttpListener.
        /// </summary>
        /// <returns></returns>
        private HttpListener CreateHttpListener()
        {
            HttpListener aHttpListener = new HttpListener();
            aHttpListener.Prefixes.Add(myAbsoluteUri);
            return aHttpListener;
        }


        private string myAbsoluteUri;
        private Func<HttpListener> myHttpListenerFactoryMethod;
        private Action<HttpListenerContext> myConnectionHandler;

        private HttpListener myListener;
        private Thread myListeningThread;
        private volatile bool myStopListeningRequested;

        private object myListeningManipulatorLock = new object();


        private string TracedObject { get { return GetType().Name + " "; } }
    }
}