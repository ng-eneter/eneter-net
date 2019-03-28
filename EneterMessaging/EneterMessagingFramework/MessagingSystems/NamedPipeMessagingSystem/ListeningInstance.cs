/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if !XAMARIN && !NETSTANDARD20

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.DataProcessing.Streaming;
using Eneter.Messaging.Threading;

namespace Eneter.Messaging.MessagingSystems.NamedPipeMessagingSystem
{
    internal class ListeningInstance
    {
        public ListeningInstance(string pipeName, int maxNumPipeInstancies, PipeSecurity pipeSecurity)
        {
            using (EneterTrace.Entering())
            {
                myPipeName = pipeName;
                myMaxNumPipeInstancies = maxNumPipeInstancies;
                myPipeSecurity = pipeSecurity;
            }
        }

        /// <summary>
        /// Starts the thread where the instance loops trying to serve client requests.
        /// </summary>
        public void StartListening(Action<Stream> messageHandler)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myListeningManipulatorLock))
                {
                    if (IsListening)
                    {
                        EneterTrace.Error(ErrorHandler.IsAlreadyListening);
                        throw new InvalidOperationException(ErrorHandler.IsAlreadyListening);
                    }

                    if (messageHandler == null)
                    {
                        throw new ArgumentNullException("The input parameter messageHandler is null.");
                    }

                    myStopListeningRequestFlag = false;
                    myMessageHandler = messageHandler;

                    // Note: It must be PipeOptions.Asynchronous because otherwise it is not possible
                    //       to close the pipe and interrupt so the waiting.
                    myPipeServer = new NamedPipeServerStream(myPipeName, PipeDirection.In, myMaxNumPipeInstancies, PipeTransmissionMode.Byte, PipeOptions.Asynchronous, 32768, 0, myPipeSecurity);

                    myListeningThread = new Thread(DoListening);
                    myListeningThread.Start();
                }
            }
        }

        /// <summary>
        /// Sets the stop listening flag before call StopListening().
        /// The problem is that the StopListening() stops some listening instance - not neccessarily this one.
        /// Therefore, we let know to all of them that the sop will come.
        /// </summary>
        private void SetStopListeningFlag()
        {
            using (EneterTrace.Entering())
            {
                myStopListeningRequestFlag = true;
            }
        }

        private void StopListening()
        {
            using (EneterTrace.Entering())
            {
                if (myPipeServer != null)
                {
                    //myPipeServer.Disconnect();
                    myPipeServer.Close();
                    myPipeServer.Dispose();
                    myPipeServer = null;
                }

                if (myListeningThread != null && myListeningThread.ThreadState != ThreadState.Unstarted)
                {
                    if (!myListeningThread.Join(300))
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.FailedToStopThreadId + myListeningThread.ManagedThreadId);

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
            }
        }


        /// <summary>
        /// Stops listening of one instance listening to this pipe.
        /// </summary>
        public static void StopListening(string pipeName, List<ListeningInstance> listeningInstances)
        {
            using (EneterTrace.Entering())
            {
                if (listeningInstances.Count > 0)
                {
                    // Indicate stop listening to all instances.
                    listeningInstances.ForEach(x => x.SetStopListeningFlag());

                    // Trigger fake clients to release 
                    for (int i = 0; i < listeningInstances.Count; ++i)
                    {
                        using (NamedPipeClientStream aFakePipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.None))
                        {
                            try
                            {
                                // If the waiting for the connection is hanging then now it will be released.
                                // If not then a timeout exception will be thrown that we will not take care
                                // because our interest is just to release the hanging thread.
                                aFakePipeClient.Connect(300);
                            }
                            catch
                            {
                                // ignore exceptions from the fake client.
                            }
                        }
                    }

                    // Wait until all listening instances stops.
                    listeningInstances.ForEach(x => x.StopListening());
                }
            }
        }

        public bool IsListening
        {
            get
            {
                using (ThreadLock.Lock(myListeningManipulatorLock))
                {
                    return myListeningThread != null;
                }
            }
        }

        /// <summary>
        /// Listens to client requests and put them to the queue from where the working thread takes them
        /// and notifies the call-backs the pipe input channel.
        /// </summary>
        private void DoListening()
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    // Reconnect the server pipe in the loop.
                    // If the client is not available the thread will wait forewer on WaitForConnection().
                    // When the listening is stopped then the thread is correctly released.
                    while (!myStopListeningRequestFlag)
                    {
                        myPipeServer.WaitForConnection();

                        DynamicStream aDynamicStream = null;
                        try
                        {
                            aDynamicStream = new DynamicStream();

                            // Start thread for reading received messages.
                            EneterThreadPool.QueueUserWorkItem(() =>
                                {
                                    while (!myStopListeningRequestFlag && myPipeServer.IsConnected)
                                    {
                                        // Read the whole message.
                                        try
                                        {
                                            myMessageHandler(aDynamicStream);
                                        }
                                        catch (Exception err)
                                        {
                                            EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                                        }
                                    }

                                    aDynamicStream.Close();
                                });

                            // Write incoming messages to the dynamic stream from where the reading thread will notify them.
                            if (!myStopListeningRequestFlag && myPipeServer.IsConnected)
                            {
                                // Read the whole message.
                                try
                                {
                                    StreamUtil.ReadToEndWithoutCopying(myPipeServer, aDynamicStream);
                                }
                                catch (Exception err)
                                {
                                    EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                                }
                            }
                        }
                        finally
                        {
                            if (aDynamicStream != null)
                            {
                                // Note: Do not close the dynamic stream here!
                                //       There can be data read before the closing and the client can still finish to read them.
                                //aDynamicStream.Close();

                                // Unblock the reading thread that can be waiting for messages.
                                aDynamicStream.IsBlockingMode = false;
                            }
                        }

                        // Disconnect the client.
                        if (!myStopListeningRequestFlag)
                        {
                            myPipeServer.Disconnect();
                        }
                    }
                }
                catch (Exception err)
                {
                    // if the error is not caused by closed communication.
                    if (!myStopListeningRequestFlag)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.FailedInListeningLoop, err);
                    }
                }
            }
        }

        private Thread myListeningThread;
        private Action<Stream> myMessageHandler;

        private string myPipeName;
        private int myMaxNumPipeInstancies;
        private PipeSecurity myPipeSecurity;
        private NamedPipeServerStream myPipeServer;
        private object myListeningManipulatorLock = new object();

        private volatile bool myStopListeningRequestFlag;


        private string TracedObject
        {
            get { return GetType().Name + " '" + myPipeName + "' "; }
        }
    }
}


#endif