/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/

using System;
using System.IO;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.DataProcessing.Streaming
{
    /// <summary>
    /// Internal helper functionality for reading data from a stream.
    /// </summary>
    internal static class StreamUtil
    {
        public static byte[] ReadToEnd(Stream inputStream)
        {
            using (EneterTrace.Entering())
            {
                using (MemoryStream aBuf = new MemoryStream())
                {
                    ReadToEnd(inputStream, aBuf);

                    return aBuf.ToArray();
                }
            }
        }

        public static void ReadToEnd(Stream inputStream, Stream outputStream)
        {
            using (EneterTrace.Entering())
            {
                int aSize = 0;
                byte[] aBuffer = new byte[32768];
                while ((aSize = inputStream.Read(aBuffer, 0, aBuffer.Length)) != 0)
                {
                    outputStream.Write(aBuffer, 0, aSize);
                }
            }
        }

        public static void ReadToEndWithoutCopying(Stream inputStream, DynamicStream outputStream)
        {
            using (EneterTrace.Entering())
            {
                while (true)
                {
                    byte[] aBuffer = new byte[32768];
                    int aSize = inputStream.Read(aBuffer, 0, aBuffer.Length);

                    if (aSize == 0)
                    {
                        break;
                    }

                    outputStream.WriteWithoutCopying(aBuffer, 0, aSize);
                }
            }
        }

        public static byte[] ReadBytes(Stream inputStream, int length)
        {
            using (EneterTrace.Entering())
            {
                using (MemoryStream aBuf = new MemoryStream())
                {
                    ReadBytes(inputStream, length, aBuf);

                    return aBuf.ToArray();
                }
            }
        }

        public static void ReadBytes(Stream inputStream, int length, Stream outputStream)
        {
            using (EneterTrace.Entering())
            {
                byte[] aBuffer = new byte[length];
                int aRemainingSize = length;
                while (aRemainingSize > 0)
                {
                    int aSize = inputStream.Read(aBuffer, 0, aBuffer.Length);

                    if (aSize == 0)
                    {
                        // Unexpected end of stream.
                        throw new EndOfStreamException();
                    }

                    outputStream.Write(aBuffer, 0, aSize);

                    aRemainingSize -= aSize;
                }
            }
        }
    }
}
