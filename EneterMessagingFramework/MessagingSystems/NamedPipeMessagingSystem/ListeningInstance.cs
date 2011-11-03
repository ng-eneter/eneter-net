/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

#if !SILVERLIGHT && !MONO

using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using Eneter.Messaging.DataProcessing.MessageQueueing;
using Eneter.Messaging.DataProcessing.Streaming;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.NamedPipeMessagingSystem
{
    internal class ListeningInstance
    {
        public ListeningInstance(string pipeName, int maxNumPipeInstancies, PipeSecurity pipeSecurity, WorkingThread<object> workingThread)
        {
            using (EneterTrace.Entering())
            {
                myPipeName = pipeName;
                myMaxNumPipeInstancies = maxNumPipeInstancies;
                myPipeSecurity = pipeSecurity;
                myWorkingThread = workingThread;
            }
        }

        /// <summary>
        /// Starts the thread where the instance loops trying to serve client requests.
        /// </summary>
        public void StartListening()
        {
            using (EneterTrace.Entering())
            {
                if (myListeningThread != null && myListeningThread.IsAlive)
                {
                    EneterTrace.Error(ErrorHandler.IsAlreadyListening);
                    throw new InvalidOperationException(ErrorHandler.IsAlreadyListening);
                }

                myStopListeningRequestFlag = false;

                myListeningThread = new Thread(DoListening);
                myListeningThread.Start();
            }
        }

        /// <summary>
        /// Sets the stop listening flag before call StopListening().
        /// The problem is that the StopListening() stops some listening instance - not neccessarily this one.
        /// Therefore, we let know to all of them that the sop will come.
        /// </summary>
        public void SetStopListeningFlag()
        {
            using (EneterTrace.Entering())
            {
                myStopListeningRequestFlag = true;
            }
        }

        /// <summary>
        /// Stops listening of one instance listening to this pipe.
        /// </summary>
        public static void StopListening(string pipeName, int numberOfListeningInstances)
        {
            using (EneterTrace.Entering())
            {
                for (int i = 0; i < numberOfListeningInstances; ++i)
                {
                    using (NamedPipeClientStream aFakePipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous))
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
                        using (NamedPipeServerStream aPipeServer = new NamedPipeServerStream(myPipeName, PipeDirection.In, myMaxNumPipeInstancies, PipeTransmissionMode.Byte, PipeOptions.None, 32768, 32768, myPipeSecurity))
                        {
                            aPipeServer.WaitForConnection();

                            object aMessage = null;

                            // If the connection is established
                            if (!myStopListeningRequestFlag && aPipeServer.IsConnected)
                            {
                                using (MemoryStream aMemStream = new MemoryStream())
                                {
                                    int aSize = 0;
                                    byte[] aBuffer = new byte[32768];

                                    while ((aSize = aPipeServer.Read(aBuffer, 0, aBuffer.Length)) != 0)
                                    {
                                        aMemStream.Write(aBuffer, 0, aSize);
                                    }

                                    aMemStream.Position = 0;
                                    aMessage = MessageStreamer.ReadMessage(aMemStream);
                                }

                                if (aMessage != null)
                                {
                                    myWorkingThread.EnqueueMessage(aMessage);
                                }
                            }
                        }
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.DoListeningFailure, err);
                }
            }
        }

        private string TracedObject
        {
            get { return "The named pipe listening instance '" + myPipeName + "' "; }
        }

        private Thread myListeningThread;

        private string myPipeName;
        private int myMaxNumPipeInstancies;
        private WorkingThread<object> myWorkingThread;
        private PipeSecurity myPipeSecurity;

        private volatile bool myStopListeningRequestFlag;
    }
}


#endif