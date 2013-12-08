/*
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
    internal class SharedMemorySender : ISender, IDisposable
    {
        public SharedMemorySender(string memoryMappedFileName, bool useExistingMemoryMappedFile, int maxMessageSize, MemoryMappedFileSecurity memoryMappedFileSecurity)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    myUseExistingMemoryMappedFile = useExistingMemoryMappedFile;
                    myMaxMessageSize = maxMessageSize;

                    // If the sender will work with already created shared memory.
                    if (useExistingMemoryMappedFile)
                    {
                        mySharedMemoryReady = TryOpenExistingEventWaitHandle(memoryMappedFileName + "_SharedMemoryReady");
                        mySharedMemoryFilled = TryOpenExistingEventWaitHandle(memoryMappedFileName + "_SharedMemoryFilled");
                        mySharedMemoryCreated = TryOpenExistingEventWaitHandle(memoryMappedFileName + "_ResponseSharedMemoryCreated");
                    }
                    else
                    {
                        EventWaitHandleSecurity anEventWaitHandleSecurity = null;
                        if (memoryMappedFileSecurity != null)
                        {
                            // Create the security for the WaitHandle logic.
                            anEventWaitHandleSecurity = new EventWaitHandleSecurity();
                            anEventWaitHandleSecurity.SetSecurityDescriptorSddlForm("S:(ML;;NW;;;LW)");
                            SecurityIdentifier aSid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
                            EventWaitHandleAccessRule anAccessRule = new EventWaitHandleAccessRule(aSid,
                                EventWaitHandleRights.Modify | EventWaitHandleRights.Synchronize,
                                AccessControlType.Allow);
                            anEventWaitHandleSecurity.AddAccessRule(anAccessRule);
                        }

                        bool aDummy;
                        mySharedMemoryReady = new EventWaitHandle(false, EventResetMode.AutoReset, memoryMappedFileName + "_SharedMemoryReady", out aDummy, anEventWaitHandleSecurity);
                        mySharedMemoryFilled = new EventWaitHandle(false, EventResetMode.AutoReset, memoryMappedFileName + "_SharedMemoryFilled", out aDummy, anEventWaitHandleSecurity);
                        mySharedMemoryCreated = new EventWaitHandle(false, EventResetMode.ManualReset, memoryMappedFileName + "_ResponseSharedMemoryCreated", out aDummy, anEventWaitHandleSecurity);
                    }

                    if (useExistingMemoryMappedFile)
                    {
                        // Wait until the memory mapped file exist.
                        // Note: The file shall be created by SharedMemorySender.
                        if (!mySharedMemoryCreated.WaitOne(3000))
                        {
                            throw new InvalidOperationException(TracedObject + "failed to open memory mapped file because it does not exist.");
                        }

                        myMemoryMappedFile = MemoryMappedFile.OpenExisting(memoryMappedFileName);
                    }
                    else
                    {
                        myMemoryMappedFile = MemoryMappedFile.CreateNew(memoryMappedFileName, maxMessageSize, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, memoryMappedFileSecurity, HandleInheritability.None);

                        // Indicate the shared memory was created.
                        mySharedMemoryCreated.Set();
                    }
                    myMemoryMappedFileStream = myMemoryMappedFile.CreateViewStream();
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + "failed to create SharedMemorySender.", err);

                    // Dispose resources alocated so far.
                    Dispose();

                    throw;
                }
            }
        }

        public void Dispose()
        {
            using (EneterTrace.Entering())
            {
                if (myMemoryMappedFileStream != null)
                {
                    myMemoryMappedFileStream.Dispose();
                    myMemoryMappedFileStream = null;
                }

                if (!myUseExistingMemoryMappedFile)
                {
                    // This sender created the memory mapped file so it must indicate it is closed.
                    if (mySharedMemoryCreated != null)
                    {
                        mySharedMemoryCreated.Reset();
                    }
                }

                if (myMemoryMappedFile != null)
                {
                    myMemoryMappedFile.Dispose();
                    myMemoryMappedFile = null;
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

                if (mySharedMemoryCreated != null)
                {
                    mySharedMemoryCreated.Dispose();
                    mySharedMemoryCreated = null;
                }
            }
        }

        public bool IsStreamWritter { get { return true; } }
        

        public void SendMessage(object message)
        {
            throw new NotSupportedException("Sending of byte[] is not supported.");
        }

        public void SendMessage(Action<Stream> toStreamWritter)
        {
            using (EneterTrace.Entering())
            {
                // Wait until the shared memory is ready for new data.
                if (!mySharedMemoryCreated.WaitOne(0) || !mySharedMemoryReady.WaitOne(5000))
                {
                    string anErrorMessage = TracedObject + "failed to send the message because the SharedMemoryReceiver was not available.";
                    throw new InvalidOperationException(anErrorMessage);
                }

                try
                {
                    // Write the message to the stream.
                    myMemoryMappedFileStream.Position = 0;

                    toStreamWritter(myMemoryMappedFileStream);

                    // Indicate new data is put to the stream.
                    mySharedMemoryFilled.Set();
                }
                catch
                {
                    // The writing to the shared memory failed.
                    // Therefore we must reset back that the shared memory is ready for messages.
                    // Otherwise it would not be possible to send next messages.
                    mySharedMemoryReady.Set();

                    throw;
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


        private bool myUseExistingMemoryMappedFile;
        private MemoryMappedFile myMemoryMappedFile;
        private MemoryMappedViewStream myMemoryMappedFileStream;

        private EventWaitHandle mySharedMemoryCreated;
        private EventWaitHandle mySharedMemoryReady;
        private EventWaitHandle mySharedMemoryFilled;

        private int myMaxMessageSize;

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}


#endif