/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using System.Collections;
using System.IO;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.DataProcessing.Streaming
{
    /// <summary>
    /// Provides functionality to read and write specific messages internaly used by duplex channels.
    /// </summary>
    public static class MessageStreamer
    {
        /// <summary>
        /// Reads the message from the stream.
        /// </summary>
        /// <param name="readingStream">stream to be read</param>
        /// <returns>message</returns>
        public static object ReadMessage(Stream readingStream)
        {
            using (EneterTrace.Entering())
            {
                BinaryReader aStreamReader = new BinaryReader(readingStream);
                object aMessage = ReadMessage(aStreamReader);

                return aMessage;
            }
        }

        private static object ReadMessage(BinaryReader reader)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    // Read the type.
                    byte aType = reader.ReadByte();
                    if (aType == default(byte))
                    {
                        // If the stream was closed
                        return null;
                    }

                    // Read the count.
                    int aNumberOfParts = 0;
                    if (aType == OBJECTS || aType == BYTES || aType == CHARS)
                    {
                        aNumberOfParts = reader.ReadInt32();
                        if (aNumberOfParts == default(int))
                        {
                            // If the stream was closed
                            return null;
                        }
                    }

                    // Read the content.
                    if (aType == OBJECTS)
                    {
                        object[] anObjects = new object[aNumberOfParts];

                        for (int i = 0; i < aNumberOfParts; ++i)
                        {
                            anObjects[i] = ReadMessage(reader);

                            // If the stream was closed
                            if (anObjects[i] == null)
                            {
                                return null;
                            }
                        }

                        return anObjects;
                    }
                    else if (aType == BYTES)
                    {
                        byte[] aBytes = reader.ReadBytes(aNumberOfParts);
                        return aBytes;
                    }
                    else if (aType == BYTE)
                    {
                        byte aByte = reader.ReadByte();
                        return aByte;
                    }
                    else if (aType == STRING)
                    {
                        string aString = reader.ReadString();
                        return aString;
                    }
                    else if (aType == CHARS)
                    {
                        char[] aChars = reader.ReadChars(aNumberOfParts);
                        return aChars;
                    }
                }
                catch (EndOfStreamException)
                {
                    return null;
                }

                string anErrorMessage = "Reading the message from the stream failed. If the serialized message consists of more serialized parts these parts must be object[] or byte[] or string or char[].";
                EneterTrace.Error(anErrorMessage);
                throw new InvalidOperationException(anErrorMessage);
            }
        }

        /// <summary>
        /// Writes the message to the specified stream.
        /// </summary>
        /// <param name="writingStream"></param>
        /// <param name="message"></param>
        public static void WriteMessage(Stream writingStream, object message)
        {
            using (EneterTrace.Entering())
            {
                BinaryWriter aStreamWriter = new BinaryWriter(writingStream);
                WriteMessage(aStreamWriter, message);
            }
        }

        private static void WriteMessage(BinaryWriter writer, object message)
        {
            using (EneterTrace.Entering())
            {
                if (message is ICollection)
                {
                    // Store number of parts
                    int aNumberOfParts = ((ICollection)message).Count;

                    // Store type of parts
                    if (message is object[])
                    {
                        writer.Write(OBJECTS);
                        writer.Write(aNumberOfParts);

                        // Go recursively down through all parts of the message.
                        foreach (object o in (object[])message)
                        {
                            WriteMessage(writer, o);
                        }
                    }
                    else if (message is byte[])
                    {
                        writer.Write(BYTES);
                        writer.Write(aNumberOfParts);
                        writer.Write((byte[])message);
                    }
                    else if (message is char[])
                    {
                        writer.Write(CHARS);
                        writer.Write(aNumberOfParts);
                        writer.Write((char[])message);
                    }
                    else
                    {
                        string anErrorMessage = "Writing the message to the stream failed. If the serialized message consists of more serialized parts these parts must be object[] or byte[] or string or char[].";
                        EneterTrace.Error(anErrorMessage);
                        throw new InvalidOperationException(anErrorMessage);
                    }
                }
                else if (message is string)
                {
                    writer.Write(STRING);
                    writer.Write((string)message);
                }
                else if (message is byte)
                {
                    writer.Write(BYTE);
                    writer.Write((byte)message);
                }
                else
                {
                    string anErrorMessage = "It is not possible to get number of message parts because it is not type of ICollection.";
                    EneterTrace.Error(anErrorMessage);
                    throw new InvalidOperationException(anErrorMessage);
                }
            }
        }

        /// <summary>
        /// Writes the message used by duplex output channels to open connection with the duplex input channel.
        /// </summary>
        /// <param name="writingStream">Stream where the message is written.</param>
        /// <param name="responseReceiverId">Id of receiver of response messages.</param>
        public static void WriteOpenConnectionMessage(Stream writingStream, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                object[] aMessage = GetOpenConnectionMessage(responseReceiverId);
                WriteMessage(writingStream, aMessage);
            }
        }

        /// <summary>
        /// Returns the low level communication message used by duplex output channels to open
        /// connection with duplex input channel.
        /// </summary>
        /// <param name="responseReceiverId"></param>
        /// <returns>Communic</returns>
        public static object[] GetOpenConnectionMessage(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                object[] aMessage = { (byte)OPENCONNECION, responseReceiverId };
                return aMessage;
            }
        }

        /// <summary>
        /// Returns true if the given message is the open connection message used by the duplex output channel
        /// to open connection.
        /// </summary>
        /// <param name="message">Message</param>
        /// <returns></returns>
        public static bool IsOpenConnectionMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                if (message is object[] &&
                    ((object[])message).Length == 2 && ((object[])message)[0] is byte &&
                    (byte)((object[])message)[0] == OPENCONNECION)
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Writes the message used by the duplex output channel to close the connection with the duplex input channel.
        /// </summary>
        /// <param name="writingStream">Stream where the message is written.</param>
        /// <param name="responseReceiverId">Id of receiver of response messages.</param>
        public static void WriteCloseConnectionMessage(Stream writingStream, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                object[] aMessage = GetCloseConnectionMessage(responseReceiverId);
                WriteMessage(writingStream, aMessage);
            }
        }

        /// <summary>
        /// Returns the low level communication message used by duplex output channel to close
        /// the connection with duplex input channel.
        /// </summary>
        /// <param name="responseReceiverId"></param>
        /// <returns></returns>
        public static object[] GetCloseConnectionMessage(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                object[] aMessage = { (byte)CLOSECONNECTION, responseReceiverId };
                return aMessage;
            }
        }

        /// <summary>
        /// Returns true if the given message is the message used by the duplex output channel to close
        /// the connection.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static bool IsCloseConnectionMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                if (message is object[] &&
                    ((object[])message).Length == 2 && ((object[])message)[0] is byte &&
                    (byte)((object[])message)[0] == CLOSECONNECTION)
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Writes the request message used by the duplex output channel to send to the duplex input channel.
        /// </summary>
        /// <param name="writingStream">Stream where the message is written.</param>
        /// <param name="responseReceiverId">Id of receiver of response messages.</param>
        /// <param name="message">Request message.</param>
        public static void WriteRequestMessage(Stream writingStream, string responseReceiverId, object message)
        {
            using (EneterTrace.Entering())
            {
                object[] aMessage = { (byte)REQUEST, responseReceiverId, message };
                WriteMessage(writingStream, aMessage);
            }
        }

        /// <summary>
        /// Returns true if the given message is the request message used by the duplex output channel to send
        /// a message to the duplex input channel.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static bool IsRequestMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                if (message is object[] &&
                    ((object[])message).Length == 3 && ((object[])message)[0] is byte &&
                    (byte)((object[])message)[0] == REQUEST)
                {
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Writes the message used by the duplex output channel to poll response messages from the
        /// duplex input channel. The message is used in case of Http messaging.
        /// </summary>
        /// <param name="writingStream">Stream where the message is written.</param>
        /// <param name="responseReceiverId">Id of receiver of response messages.</param>
        public static void WritePollResponseMessage(Stream writingStream, string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                WriteMessage(writingStream, responseReceiverId);
            }
        }

        /// <summary>
        /// Returns true if the given message is the essage used by the duplex output channel to poll
        /// messages from the duplex input channel. (in case of Http)
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static bool IsPollResponseMessage(object message)
        {
            using (EneterTrace.Entering())
            {
                return message is string;
            }
        }


        // Type of serialization.
        private const byte OBJECTS = 01;
        private const byte BYTES = 02;
        private const byte BYTE = 03;
        private const byte STRING = 04;
        private const byte CHARS = 05;

        // Type of low level message - used for the low level communication between channels.
        private const byte OPENCONNECION = 0;
        private const byte CLOSECONNECTION = 1;
        private const byte REQUEST = 2;
    }
}
