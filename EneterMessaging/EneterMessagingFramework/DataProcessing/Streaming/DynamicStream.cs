

using System;
using System.IO;
using Eneter.Messaging.DataProcessing.MessageQueueing;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.DataProcessing.Streaming
{
    /// <summary>
    /// Stream which can be written and read at the same time.
    /// </summary>
    /// <remarks>
    /// The dynamic stream supports writing of data by one thread and reading by another.
    /// The reading operation is blocked until the requested amount of data is not available.
    /// </remarks>
    public class DynamicStream : Stream
    {
        /// <summary>
        /// Gets or sets the blocking mode. If blocking mode then Read method blocks until data is available.
        /// </summary>
        /// <remarks>
        /// If blocking mode then Read method blocks until data is available. If unblocking mode is set then
        /// Read method reads available data and returns. If a reading thread is blocked (waiting) and the unblocking mode
        /// is set the thread is released.
        /// </remarks>
        public bool IsBlockingMode
        {
            get
            {
                return myMessageQueue.IsBlockingMode;
            }

            set
            {
                if (value == true)
                {
                    myMessageQueue.BlockProcessingThreads();
                }
                else
                {
                    myMessageQueue.UnblockProcessingThreads();
                }
            }
        }

        /// <summary>
        /// Returns true, because the stream supports reading.
        /// </summary>
        public override bool CanRead { get { return true; } }

        /// <summary>
        /// Returns false, because the stream does not support Seek.
        /// </summary>
        public override bool CanSeek { get { return false; } }

        /// <summary>
        /// Returns true, because the stream supports writing.
        /// </summary>
        public override bool CanWrite { get { return true; } }

        /// <summary>
        /// The Flush is not applicable. If called, it does nothing.
        /// </summary>
        public override void Flush()
        {
        }

        /// <summary>
        /// Returns always 0.
        /// </summary>
        public override long Length
        {
            //get { throw new NotSupportedException(); }
            get { return 0; }
        }

        /// <summary>
        /// 'Get' returns always 0. Set does nothing.
        /// </summary>
        public override long Position
        {
            get { return 0; }
            set {}
        }

        /// <summary>
        /// The stream does not support Seek.
        /// It throws NotSupportedException.
        /// </summary>
        /// <param name="offset">not applicable</param>
        /// <param name="origin">not applicable</param>
        /// <returns>not applicable</returns>
        /// <exception cref="NotSupportedException">
        /// </exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// The stream does not support SetLength.
        /// It throws NotSupportedException.
        /// </summary>
        /// <param name="value">not applicable</param>
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Reads data from the stream to the specified buffer.
        /// </summary>
        /// <remarks>
        /// If the requested amount of data is not available the thread is blocked until required amount of data
        /// is available - until data is written by another thread.
        /// </remarks>
        /// <param name="buffer">The buffer where the data will be written.</param>
        /// <param name="offset">Starting position in the buffer where data will be wqritten.</param>
        /// <param name="count">Requested amount of data to be read.</param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            //using (EneterTrace.Entering())
            //{
                if (myIsClosed)
                {
                    return 0;
                }

                // Only one thread can be reading.
                // The thread waits until all data is available.
                // Note: If more threads would try to read in parallel then it would be bad because the reading removes the data from the queue.
                using (ThreadLock.Lock(myLockDuringReading))
                {
                    int aReadSize = 0;

                    // If there is a memory stream that we already started to read but not finished. (i.e.: requested data ended at the middle
                    // of the memory stream.
                    if (myDataInProgress != null)
                    {
                        // Read the data from the memory stream, and store the really read size.
                        aReadSize = myDataInProgress.Read(buffer, offset, count);

                        // If we are at the end of the memory stream then close it.
                        if (myDataInProgress.Position == myDataInProgress.Length)
                        {
                            myDataInProgress.Close();
                            myDataInProgress = null;
                        }
                    }

                    // While stream is not closed and we do not read required amount of data.
                    while (!myIsClosed && aReadSize < count)
                    {
                        // Remove the sequence of bytes from the queue.
                        myDataInProgress = myMessageQueue.DequeueMessage();

                        if (myDataInProgress != null)
                        {
                            // Try to read the remaining amount of bytes from the memory stream.
                            aReadSize += myDataInProgress.Read(buffer, offset + aReadSize, count - aReadSize);

                            // If we are at the end of the memory stream then close it.
                            if (myDataInProgress.Position == myDataInProgress.Length)
                            {
                                myDataInProgress.Close();
                                myDataInProgress = null;
                            }
                        }

                        // If we are in unblocking mode then do not loop if the queue is empty.
                        if (myMessageQueue.IsBlockingMode == false && myMessageQueue.Count == 0)
                        {
                            break;
                        }
                    }

                    return aReadSize;
                }
            //}
        }

        /// <summary>
        /// Writes the data to the stream.
        /// </summary>
        /// <param name="buffer">Buffer to be written to the stream</param>
        /// <param name="offset">Starting podition in the buffer from where data will be read.</param>
        /// <param name="count">Amount of data to be read from the buffer and written to the stream.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myLockDuringWriting))
                {
                    if (myIsClosed)
                    {
                        return;
                    }

                    byte[] aData = new byte[count];
                    Buffer.BlockCopy(buffer, offset, aData, 0, count);

                    MemoryStream aChunkStream = new MemoryStream(aData, false);
                    myMessageQueue.EnqueueMessage(aChunkStream);
                }
            }
        }

        /// <summary>
        /// Writes data to the stream the way that it just stores the reference to the input data.
        /// </summary>
        /// <remarks>
        /// It does not copy the incoming data to the stream but instead of that it just stores the reference.
        /// This approach is very fast but the input byte[] array should not be modified after calling this method.
        /// </remarks>
        /// <param name="data">data to be written to the stream.</param>
        /// <param name="offset">Starting position in the buffer from where data will be read.</param>
        /// <param name="count">Amount of data to be read from the buffer and written to the stream.</param>
        public void WriteWithoutCopying(byte[] data, int offset, int count)
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myLockDuringWriting))
                {
                    if (myIsClosed)
                    {
                        return;
                    }

                    MemoryStream aChunkStream = new MemoryStream(data, offset, count, false);
                    myMessageQueue.EnqueueMessage(aChunkStream);
                }
            }
        }

        /// <summary>
        /// Closes the stream and releases the reading thread waiting for data.
        /// </summary>
        public override void Close()
        {
            using (EneterTrace.Entering())
            {
                using (ThreadLock.Lock(myLockDuringWriting))
                {
                    myMessageQueue.UnblockProcessingThreads();
                    myIsClosed = true;

                    base.Close();
                }
            }
        }


        /// <summary>
        /// The writing puts the byte sequences to the queue as they come.
        /// The reading removes the sequences of bytes from the queue.
        /// </summary>
        private MessageQueue<MemoryStream> myMessageQueue = new MessageQueue<MemoryStream>();
        private object myLockDuringReading = new object();
        private object myLockDuringWriting = new object();

        // Make it open after constructor.
        private volatile bool myIsClosed = false;

        private MemoryStream myDataInProgress = null;
    }
}
