/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/


#if NET4

using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;
using Eneter.Messaging.DataProcessing.Streaming;
using Eneter.Messaging.Diagnostic;
using Eneter.Messaging.MessagingSystems.MessagingSystemBase;

namespace Eneter.Messaging.MessagingSystems.SharedMemoryMessagingSystem
{
    internal class SharedMemoryOutputChannel : IOutputChannel
    {
        public SharedMemoryOutputChannel(string channelId)
        {
            using (EneterTrace.Entering())
            {
                ChannelId = channelId;
            }
        }

        public string ChannelId { get; private set; }
        

        public void SendMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    // Put the message to the memory stream.
                    // Note: It is faster to put it to the memory stream as to use the BinaryWriter
                    //       directly on the 'MemoryMappedViewStream'.
                    byte[] aBufferedMessage = null;
                    using (MemoryStream aMemStream = new MemoryStream())
                    {
                        MessageStreamer.WriteMessage(aMemStream, message);
                        aBufferedMessage = aMemStream.ToArray();
                    }

                    // If the shared memory is ready for messages, then write the message.
                    EventWaitHandle aSharedMemoryCleared = new EventWaitHandle(false, EventResetMode.AutoReset, ChannelId + "_SharedMemoryReady");
                    if (!aSharedMemoryCleared.WaitOne(30000))
                    {
                        string anErrorMessage = TracedObject + "failed to send the message because the input channel was not available.";
                        EneterTrace.Error(anErrorMessage);
                        throw new InvalidOperationException(anErrorMessage);
                    }

                    try
                    {
                        using (MemoryMappedFile aMemoryFile = MemoryMappedFile.OpenExisting(ChannelId))
                        {
                            using (MemoryMappedViewStream aStream = aMemoryFile.CreateViewStream())
                            {
                                aStream.Write(aBufferedMessage, 0, aBufferedMessage.Length);
                            }
                        }

                        // Indicate to the listening input channel that the message was put to the shared memory.
                        EventWaitHandle aSharedMemoryOccupied = new EventWaitHandle(false, EventResetMode.AutoReset, ChannelId + "_SharedMemoryFilled");
                        aSharedMemoryOccupied.Set();
                    }
                    catch
                    {
                        // The writing to the shared memory failed.
                        // Therefore we must reset back that the shared memory is ready for messages.
                        // Otherwise it would not be possible to send next messages.
                        aSharedMemoryCleared.Set();

                        throw;
                    }
                }
                catch (Exception err)
                {
                    EneterTrace.Error(TracedObject + ErrorHandler.SendMessageFailure, err);
                    throw;
                }
            }
        }

        private string TracedObject
        {
            get
            {
                return "The SharedMemoryOutputChannel '" + ChannelId + "' ";
            }
        }
    }
}


#endif