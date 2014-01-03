﻿/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

#if NET4 || NET45

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase;


namespace Eneter.Messaging.MessagingSystems.SharedMemoryMessagingSystem
{
    internal class SharedMemoryReceiver
    {
        public SharedMemoryReceiver(string memoryMappedFileName, bool useExistingMemoryMappedFile, int maxMessageSize, MemoryMappedFileSecurity memmoryMappedFileSecurity)
        {
            using (EneterTrace.Entering())
            {
                myMemoryMappedFileName = memoryMappedFileName;
                myMaxMessageSize = maxMessageSize;

                // If true then the listener will not create the new memory shared file but will open existing one.
                // This is useful for security restrictions if the functionality is used in the windows service.
                myUseExistingMemoryMappedFile = useExistingMemoryMappedFile;

                myMemmoryMappedFileSecurity = memmoryMappedFileSecurity;

                // If the security shall be applied.
                if (myMemmoryMappedFileSecurity != null)
                {
                    // Create the security for the WaitHandle sync logic.
                    myEventWaitHandleSecurity = new EventWaitHandleSecurity();
                    myEventWaitHandleSecurity.SetSecurityDescriptorSddlForm("S:(ML;;NW;;;LW)");
                    SecurityIdentifier aSid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                    EventWaitHandleAccessRule anAccessRule = new EventWaitHandleAccessRule(aSid,
                        EventWaitHandleRights.Modify | EventWaitHandleRights.Synchronize,
                        AccessControlType.Allow);
                    myEventWaitHandleSecurity.AddAccessRule(anAccessRule);
                }
            }
        }

        public void StartListening(Func<MessageContext, bool> messageHandler)
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

                    if (messageHandler == null)
                    {
                        throw new ArgumentNullException("The input parameter messageHandler is null.");
                    }

                    // If this was not the first instance of the mutex, then it means, that the listener with the same
                    // memory mapped file already exists.
                    bool aFirstInstanceFlag;
                    try
                    {
                        myOnlyListeningInstance = new Mutex(false, myMemoryMappedFileName + "_OnlyListenerMutex", out aFirstInstanceFlag);
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + "failed to recognize if this is only listener.", err);
                        throw;
                    }
                    if (!aFirstInstanceFlag)
                    {
                        string anErrorMessage = TracedObject + "cannot start listening because a listener with the same memory mapped file already exists.";
                        EneterTrace.Error(anErrorMessage);
                        throw new InvalidOperationException(anErrorMessage);
                    }

                    try
                    {
                        myStopListeningRequested = false;
                        myMessageHandler = messageHandler;

                        // IF the listener will work with already existing memory mapped file.
                        if (myUseExistingMemoryMappedFile)
                        {
                            // Open existing synchronization event handles.
                            mySharedMemoryCreated = TryOpenExistingEventWaitHandle(myMemoryMappedFileName + "_ResponseSharedMemoryCreated");
                            mySharedMemoryFilled = TryOpenExistingEventWaitHandle(myMemoryMappedFileName + "_SharedMemoryFilled");
                            mySharedMemoryReady = TryOpenExistingEventWaitHandle(myMemoryMappedFileName + "_SharedMemoryReady");

                            // Wait until the memory mapped file exist.
                            // Note: The file shall be created by SharedMemorySender.
                            if (!mySharedMemoryCreated.WaitOne(3000))
                            {
                                throw new InvalidOperationException(TracedObject + "failed to open memory mapped file because it does not exist.");
                            }

                            // Open existing shared memory file.
                            myMemoryMappedFile = MemoryMappedFile.OpenExisting(myMemoryMappedFileName);
                        }
                        else
                        {
                            // Create synchronization event handles.
                            bool aDummy;
                            mySharedMemoryCreated = new EventWaitHandle(false, EventResetMode.ManualReset, myMemoryMappedFileName + "_ResponseSharedMemoryCreated", out aDummy, myEventWaitHandleSecurity);
                            mySharedMemoryFilled = new EventWaitHandle(false, EventResetMode.AutoReset, myMemoryMappedFileName + "_SharedMemoryFilled", out aDummy, myEventWaitHandleSecurity);
                            mySharedMemoryReady = new EventWaitHandle(false, EventResetMode.AutoReset, myMemoryMappedFileName + "_SharedMemoryReady", out aDummy, myEventWaitHandleSecurity);

                            // Create the new memory mapped file.
                            myMemoryMappedFile = MemoryMappedFile.CreateNew(myMemoryMappedFileName, myMaxMessageSize, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, myMemmoryMappedFileSecurity, HandleInheritability.None);

                            // Indicate the memory mapped file was created.
                            mySharedMemoryCreated.Set();
                        }

                        // Start the listener that will read messages from the shared memory.
                        myListeningThread = new Thread(DoListening);
                        myListeningThread.Start();

                        // Wait until the listening thread is ready.
                        myListeningStartedEvent.WaitOne(5000);
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
                    // Stop the thread listening to messages from the shared memory.
                    myStopListeningRequested = true;
                    try
                    {
                        if (mySharedMemoryFilled != null)
                        {
                            // Make the fake indication just to interrupt waiting in the listening thread.
                            mySharedMemoryFilled.Set();
                        }
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Warning(TracedObject + "failed to indicate the listening thread shall stop.", err);
                    }

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
                    }
                    myListeningThread = null;

                    // Close the shared memory.
                    CloseSharedMemory();
                }
            }
        }

        public bool IsListening
        {
            get
            {
                lock (myListeningManipulatorLock)
                {
                    return myMemoryMappedFile != null;
                }
            }
        }

        private void DoListening()
        {
            using (EneterTrace.Entering())
            {
                bool anErrorFlag = false;

                try
                {
                    using (MemoryMappedViewStream aSharedMemoryStream = myMemoryMappedFile.CreateViewStream())
                    {
                        // Indicate the shared memory is ready to receive data.
                        mySharedMemoryReady.Set();

                        // Indicate the listening is running.
                        myListeningStartedEvent.Set();

                        // Loop until the stop is requested.
                        while (!myStopListeningRequested)
                        {
                            // Wait until the sender indicates the message was put to the shared memory.
                            // If not trafic then check the connection.
                            if (!mySharedMemoryFilled.WaitOne(500))
                            {
                                // If the memory mapped file is created by SharedMemorySender then it can hapen
                                // the file was closed. So check if the file is still open.
                                if (myUseExistingMemoryMappedFile)
                                {
                                    if (!mySharedMemoryCreated.WaitOne(0))
                                    {
                                        anErrorFlag = true;
                                        break;
                                    }
                                }

                                continue;
                            }

                            if (myStopListeningRequested)
                            {
                                break;
                            }

                            try
                            {
                                aSharedMemoryStream.Position = 0;

                                // Note: The handler should read the message from the stream and process it in a different
                                //       thread. So that the 'ready' state can be indicated as faast as possible.
                                MessageContext aMessageContext = new MessageContext(aSharedMemoryStream, "", null);
                                myMessageHandler(aMessageContext);
                            }
                            catch (Exception err)
                            {
                                EneterTrace.Warning(TracedObject + ErrorHandler.DetectedException, err);
                            }

                            // Indicate the shared memory is ready for new data.
                            mySharedMemoryReady.Set();
                        }
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.DoListeningFailure, err);

                    anErrorFlag = true;
                }

                if (anErrorFlag)
                {
                    Func<MessageContext, bool> aMessageHandler = myMessageHandler;

                    CloseSharedMemory();

                    // Inform message handler callback that the shared memory listener was closed.
                    try
                    {
                        if (aMessageHandler != null)
                        {
                            aMessageHandler(null);
                        }
                    }
                    catch (Exception err)
                    {
                        EneterTrace.Error(TracedObject + ErrorHandler.DetectedException, err);
                    }
                }

                // Indicate the listening is stopped.
                myListeningStartedEvent.Reset();
            }
        }

        private void CloseSharedMemory()
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    if (mySharedMemoryCreated != null)
                    {
                        // Close the shared memory.
                        if (!myUseExistingMemoryMappedFile)
                        {
                            // This listener created the memory mapped file so it must indicate the memory mapped file is closed.
                            mySharedMemoryCreated.Reset();
                        }

                        mySharedMemoryCreated.Dispose();
                        mySharedMemoryCreated = null;
                    }

                    if (mySharedMemoryFilled != null)
                    {
                        mySharedMemoryFilled.Dispose();
                        mySharedMemoryFilled = null;
                    }

                    if (mySharedMemoryReady != null)
                    {
                        mySharedMemoryReady.Dispose();
                        mySharedMemoryReady = null;
                    }

                    if (myMemoryMappedFile != null)
                    {
                        myMemoryMappedFile.Dispose();
                        myMemoryMappedFile = null;

                        // Delete the mutex ensuring only one listening instance.
                        // After that some other listener can be started.
                        myOnlyListeningInstance.Dispose();
                        myOnlyListeningInstance = null;
                    }

                    // Release the reference to the callback processing messages.
                    myMessageHandler = null;
                }
                catch (Exception err)
                {
                    EneterTrace.Warning(TracedObject + "failed to close shared memory.", err);
                }
            }
        }

        private EventWaitHandle TryOpenExistingEventWaitHandle(string name)
        {
            using (EneterTrace.Entering())
            {
                EventWaitHandle anEventWaitHandle = null;

                for (int i = 30; i >= 0; --i)
                {

                    try
                    {
                        anEventWaitHandle = EventWaitHandle.OpenExisting(name);

                        // No exception, so the handle was open.
                        break;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // The handle exists but we do not have enough access rights to use it.
                        throw;
                    }
                    catch (WaitHandleCannotBeOpenedException)
                    {
                        // The handle does not exist.
                        if (i == 0)
                        {
                            throw;
                        }
                    }

                    // Wait a moment and try again. The handle can be meanwhile created.
                    Thread.Sleep(100);
                }

                return anEventWaitHandle;
            }
        }


        private Func<MessageContext, bool> myMessageHandler;

        private volatile bool myStopListeningRequested;
        private ManualResetEvent myListeningStartedEvent = new ManualResetEvent(false);
        private Thread myListeningThread;

        private Mutex myOnlyListeningInstance;
        private EventWaitHandle mySharedMemoryCreated;
        private EventWaitHandle mySharedMemoryReady;
        private EventWaitHandle mySharedMemoryFilled;
        private EventWaitHandleSecurity myEventWaitHandleSecurity;
        private MemoryMappedFileSecurity myMemmoryMappedFileSecurity;

        private int myMaxMessageSize;
        private bool myUseExistingMemoryMappedFile;
        private string myMemoryMappedFileName;
        private MemoryMappedFile myMemoryMappedFile;

        private object myListeningManipulatorLock = new object();

        private string TracedObject { get { return GetType().Name + " "; } }
    }
}

#endif