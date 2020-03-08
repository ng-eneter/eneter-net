/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/

#if !NET35 && NETFRAMEWORK

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.SharedMemoryMessagingSystem
{
    internal class SharedMemorySender : IDisposable
    {
        // Note: in order to be consistent with other messagings (e.g. TcpMessaging) value 0 for timeouts mean infinite time.
        public SharedMemorySender(string memoryMappedFileName, bool useExistingMemoryMappedFile, TimeSpan openTimeout, TimeSpan sendTimeout, int maxMessageSize, MemoryMappedFileSecurity memoryMappedFileSecurity)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    mySendTimeout = (sendTimeout == TimeSpan.Zero) ? TimeSpan.FromMilliseconds(-1) : sendTimeout;
                    myUseExistingMemoryMappedFile = useExistingMemoryMappedFile;
                    myMaxMessageSize = maxMessageSize;

                    // If the shared memory file is created by SharedMemoryReceiver then open existing one.
                    if (useExistingMemoryMappedFile)
                    {
                        TimeSpan anOpenTimeout = (openTimeout == TimeSpan.Zero) ? TimeSpan.FromMilliseconds(-1) : openTimeout;

                        // Try to open synchronization handlers.
                        mySharedMemoryReady = EventWaitHandleExt.OpenExisting(memoryMappedFileName + "_SharedMemoryReady", anOpenTimeout);
                        mySharedMemoryFilled = EventWaitHandleExt.OpenExisting(memoryMappedFileName + "_SharedMemoryFilled", anOpenTimeout);
                        mySharedMemoryCreated = EventWaitHandleExt.OpenExisting(memoryMappedFileName + "_ResponseSharedMemoryCreated", anOpenTimeout);

                        // Wait until the memory mapped file exist and then open it.
                        // Note: it shall be created by SharedMemoryReceiver.
                        if (!mySharedMemoryCreated.WaitOne(anOpenTimeout))
                        {
                            throw new InvalidOperationException(TracedObject + "failed to open memory mapped file because it does not exist.");
                        }
                        myMemoryMappedFile = MemoryMappedFile.OpenExisting(memoryMappedFileName);
                    }
                    // If the shared memory file shall be created by SharedMemorySender.
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

                        // Create synchronization handlers.
                        bool aDummy;
                        mySharedMemoryReady = new EventWaitHandle(false, EventResetMode.AutoReset, memoryMappedFileName + "_SharedMemoryReady", out aDummy, anEventWaitHandleSecurity);
                        mySharedMemoryFilled = new EventWaitHandle(false, EventResetMode.AutoReset, memoryMappedFileName + "_SharedMemoryFilled", out aDummy, anEventWaitHandleSecurity);
                        mySharedMemoryCreated = new EventWaitHandle(false, EventResetMode.ManualReset, memoryMappedFileName + "_ResponseSharedMemoryCreated", out aDummy, anEventWaitHandleSecurity);

                        // Create the memory mapped file.
                        myMemoryMappedFile = MemoryMappedFile.CreateNew(memoryMappedFileName, maxMessageSize, MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, memoryMappedFileSecurity, HandleInheritability.None);

                        // Indicate the shared memory is created.
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

        public void SendMessage(Action<Stream> toStreamWritter)
        {
            using (EneterTrace.Entering())
            {
                // Wait until the shared memory is ready for new data.
                if (!mySharedMemoryCreated.WaitOne(0))
                {
                    string anErrorMessage = TracedObject + "failed to send the message because the shared memory was not created or it was already destroyed.";
                    throw new InvalidOperationException(anErrorMessage);
                }

                if (!mySharedMemoryReady.WaitOne(mySendTimeout))
                {
                    string anErrorMessage = TracedObject + "failed to send the message because the shared memory was not available within the specified timeout: " + mySendTimeout;
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


        private bool myUseExistingMemoryMappedFile;
        private MemoryMappedFile myMemoryMappedFile;
        private MemoryMappedViewStream myMemoryMappedFileStream;

        private TimeSpan mySendTimeout;

        private EventWaitHandle mySharedMemoryCreated;
        private EventWaitHandle mySharedMemoryReady;
        private EventWaitHandle mySharedMemoryFilled;

        private int myMaxMessageSize;

        private string TracedObject { get { return GetType().Name + ' '; } }
    }
}


#endif