/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

#if NET4

using System;
using System.IO.MemoryMappedFiles;
using System.Threading;
using Eneter.Messaging.DataProcessing.MessageQueueing;
using Eneter.Messaging.DataProcessing.Streaming;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.SharedMemoryMessagingSystem
{
    internal class SharedMemoryInputChannel : IInputChannel
    {
        public event EventHandler<ChannelMessageEventArgs> MessageReceived;

        public SharedMemoryInputChannel(string channelId, int maxMessageSize)
        {
            ChannelId = channelId;
            myMaxMessageSize = maxMessageSize;

            myMessageProcessingThread = new WorkingThread<object>();

        }

        public string ChannelId { get; private set; }
        

        public void StartListening()
        {
            using (EneterTrace.Entering())
            {
                lock (myListeningManipulatorLock)
                {
                    if (IsListening)
                    {
                        string aMessage = TracedObject + ErrorHandler.IsAlreadyListening;
                        EneterTrace.Error(aMessage);
                        throw new InvalidOperationException(aMessage);
                    }

                    try
                    {
                        // If this was not the first instance of the mutex, then it means, that the listener with the same
                        // channel id already exists.
                        bool aFirstInstanceFlag;
                        myOnlyListeningInstance = new Mutex(false, ChannelId + "_OnlyListenerMutex", out aFirstInstanceFlag);
                        if (!aFirstInstanceFlag)
                        {
                            string anErrorMessage = TracedObject + "cannot start listening because a listener with the same channel id already exists in the computer.";
                            EneterTrace.Error(anErrorMessage);
                            throw new InvalidOperationException(anErrorMessage);
                        }

                        // Initialize queue for received messages.
                        myMessageProcessingThread.RegisterMessageHandler(MessageHandler);

                        // Create the shared memory file.
                        mySharedMemory = MemoryMappedFile.CreateNew(ChannelId, myMaxMessageSize);

                        // Start the listener that will read messages from the shared memory.
                        myStopListeningRequested = false;
                        myListeningThread = new Thread(DoListening);
                        myListeningThread.Start();
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.StartListeningFailure, err);

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
                lock (myListeningManipulatorLock)
                {
                    // Stop the thred looping and reading messages from the shared memory.
                    myStopListeningRequested = true;
                    EventWaitHandle aSharedMemoryOccupied = new EventWaitHandle(false, EventResetMode.AutoReset, ChannelId + "_SharedMemoryFilled");
                    aSharedMemoryOccupied.Set();
                    if (myListeningThread != null && myListeningThread.ThreadState != ThreadState.Unstarted)
                    {
                        if (!myListeningThread.Join(3000))
                        {
                            EneterTrace.Warning(TracedObject + ErrorHandler.StopThreadFailure + myListeningThread.ManagedThreadId.ToString());

                            try
                            {
                                myListeningThread.Abort();
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.AbortThreadFailure, err);
                            }
                        }

                        myListeningThread = null;
                    }

                    // Close the shared memory.
                    if (mySharedMemory != null)
                    {
                        mySharedMemory.Dispose();
                        mySharedMemory = null;

                        // Delete the mutex ensuring only one listening instance.
                        // After that some other listener can be started.
                        myOnlyListeningInstance.Dispose();
                        myOnlyListeningInstance = null;
                    }

                    // Stop thread processing the queue with messages.
                    try
                    {
                        myMessageProcessingThread.UnregisterMessageHandler();
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.UnregisterMessageHandlerThreadFailure, err);
                    }
                }
            }
        }

        public bool IsListening
        {
            get
            {
                using (EneterTrace.Entering())
                {
                    lock (myListeningManipulatorLock)
                    {
                        return mySharedMemory != null;
                    }
                }
            }
        }


        private void DoListening()
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    // The output channel indicates that the message was written to the shared memory.
                    EventWaitHandle aSharedMemoryFilled = new EventWaitHandle(false, EventResetMode.AutoReset, ChannelId + "_SharedMemoryFilled");

                    // This listener indicates to the output channel that the message was read from the shared memory.
                    EventWaitHandle aSharedMemoryReady = new EventWaitHandle(true, EventResetMode.AutoReset, ChannelId + "_SharedMemoryReady");

                    MemoryMappedViewStream aSharedMemoryStream = mySharedMemory.CreateViewStream();

                    // Loop until the stop is requested.
                    while (!myStopListeningRequested)
                    {
                        // Wait until the output channel indicates the message was put to the shared memory.
                        aSharedMemoryFilled.WaitOne();

                        if (myStopListeningRequested)
                        {
                            break;
                        }

                        object aMessage = null;

                        try
                        {
                            aMessage = MessageStreamer.ReadMessage(aSharedMemoryStream);
                            aSharedMemoryStream.Position = 0;
                        }
                        finally
                        {
                            // Indicate to the output channel that the message was read.
                            aSharedMemoryReady.Set();
                        }

                        // Put the message to the queue.
                        if (aMessage != null)
                        {
                            myMessageProcessingThread.EnqueueMessage(aMessage);
                        }
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "stoped to listen to message.", err);
                }
            }
        }

        private void MessageHandler(object message)
        {
            using (EneterTrace.Entering())
            {
                if (MessageReceived != null)
                {
                    try
                    {
                        MessageReceived(this, new ChannelMessageEventArgs(ChannelId, message));
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }
                else
                {
                    EneterTrace.Warning(TracedObject + ErrorHandler.NobodySubscribedForMessage);
                }
            }
        }


        private int myMaxMessageSize;

        private object myListeningManipulatorLock = new object();

        private Mutex myOnlyListeningInstance;

        private volatile bool myStopListeningRequested;

        private WorkingThread<object> myMessageProcessingThread;
        private Thread myListeningThread;
        private MemoryMappedFile mySharedMemory;


        private string TracedObject
        {
            get
            {
                return "The SharedMemoryInputChannel '" + ChannelId + "' ";
            }
        }
    }
}

#endif