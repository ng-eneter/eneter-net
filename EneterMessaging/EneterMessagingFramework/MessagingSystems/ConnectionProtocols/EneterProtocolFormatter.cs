/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System;
using System.IO;
using System.Text;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.ConnectionProtocols
{
    /// <summary>
    /// Implements encoding/decoding of low-level messages into eneter format.
    /// </summary>
    public class EneterProtocolFormatter : IProtocolFormatter
    {
        /// <summary>
        /// Encodes open connection request.
        /// </summary>
        /// <param name="responseReceiverId">client id</param>
        /// <returns></returns>
        public object EncodeOpenConnectionMessage(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                using (MemoryStream aBuffer = new MemoryStream())
                {
                    EncodeOpenConnectionMessage(responseReceiverId, aBuffer);
                    return aBuffer.ToArray();
                }
            }
        }

        /// <summary>
        /// Encodes 'open connection' message directly to the stream.
        /// </summary>
        /// <param name="responseReceiverId"></param>
        /// <param name="outputSream"></param>
        public void EncodeOpenConnectionMessage(string responseReceiverId, Stream outputSream)
        {
            using (EneterTrace.Entering())
            {
                BinaryWriter aWriter = new BinaryWriter(outputSream);
                    
                EncodeHeader(aWriter);

                aWriter.Write(OPEN_CONNECTION_REQUEST);

                EncodeString(aWriter, responseReceiverId);
            }
        }

        /// <summary>
        /// Encodes close connection request.
        /// </summary>
        /// <param name="responseReceiverId">client id</param>
        /// <returns></returns>
        public object EncodeCloseConnectionMessage(string responseReceiverId)
        {
            using (EneterTrace.Entering())
            {
                using (MemoryStream aBuffer = new MemoryStream())
                {
                    EncodeCloseConnectionMessage(responseReceiverId, aBuffer);
                    return aBuffer.ToArray();
                }
            }
        }

        /// <summary>
        /// Encodes 'close connection' message directly to the stream.
        /// </summary>
        /// <param name="responseReceiverId"></param>
        /// <param name="outputSream"></param>
        public void EncodeCloseConnectionMessage(string responseReceiverId, Stream outputSream)
        {
            using (EneterTrace.Entering())
            {
                BinaryWriter aWriter = new BinaryWriter(outputSream);

                EncodeHeader(aWriter);

                aWriter.Write(CLOSE_CONNECTION_REQUEST);

                EncodeString(aWriter, responseReceiverId);
            }
        }

        /// <summary>
        /// Encodes a message or a response message.
        /// </summary>
        /// <param name="responseReceiverId">client id</param>
        /// <param name="message">the serialized content of the message</param>
        /// <returns></returns>
        public object EncodeMessage(string responseReceiverId, object message)
        {
            using (EneterTrace.Entering())
            {
                using (MemoryStream aBuffer = new MemoryStream())
                {
                    EncodeMessage(responseReceiverId, message, aBuffer);
                    return aBuffer.ToArray();
                }
            }
        }

        /// <summary>
        /// Encodes 'request message' or 'response message' directly to the stream.
        /// </summary>
        /// <param name="responseReceiverId"></param>
        /// <param name="message"></param>
        /// <param name="outputSream"></param>
        public void EncodeMessage(string responseReceiverId, object message, Stream outputSream)
        {
            using (EneterTrace.Entering())
            {
                BinaryWriter aWriter = new BinaryWriter(outputSream);

                EncodeHeader(aWriter);

                aWriter.Write(REQUEST_MESSAGE);

                EncodeString(aWriter, responseReceiverId);

                EncodeMessage(aWriter, message);
            }
        }

        /// <summary>
        /// Decodes a message or a response message from the stream.
        /// </summary>
        /// <param name="readStream">stream containing the low-level message</param>
        /// <returns></returns>
        public ProtocolMessage DecodeMessage(Stream readStream)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    // Note: Do not enclose the BinaryReader with using because it will close the stream!!!
                    BinaryReader aReader = new BinaryReader(readStream);

                    byte[] h = aReader.ReadBytes(6);
                    if (h.Length < 6)
                    {
                        throw new EndOfStreamException(TracedObject + "detected end of stream during reading of Eneter header.");
                    }

                    // Read the header to recognize if it is the correct protocol.
                    if (h[0] != 'E' || h[1] != 'N' || h[2] != 'E' || h[3] != 'T' || h[4] != 'E' || h[5] != 'R')
                    {
                        EneterTrace.Warning(TracedObject + "detected unknown protocol format.");
                        return null;
                    }

                    // Get endian encoding (Big Endian or Little Endian)
                    int anEndianEncodingId = aReader.ReadByte();
                    if (anEndianEncodingId != LITTLE_ENDIAN && anEndianEncodingId != BIG_ENDIAN)
                    {
                        EneterTrace.Warning(TracedObject + "detected unknown endian encoding.");
                        return null;
                    }


                    // Get string encoding (UTF8 or UTF16)
                    int aStringEncodingId = aReader.ReadByte();
                    if (aStringEncodingId != UTF8 && aStringEncodingId != UTF16)
                    {
                        EneterTrace.Warning(TracedObject + "detected unknown string encoding.");
                        return null;
                    }


                    // Get the message type.
                    int aMessageType = aReader.ReadByte();


                    ProtocolMessage aProtocolMessage;

                    if (aMessageType == OPEN_CONNECTION_REQUEST)
                    {
                        aProtocolMessage = DecodeRequest(EProtocolMessageType.OpenConnectionRequest, aReader, anEndianEncodingId, aStringEncodingId);
                    }
                    else if (aMessageType == CLOSE_CONNECTION_REQUEST)
                    {
                        aProtocolMessage = DecodeRequest(EProtocolMessageType.CloseConnectionRequest, aReader, anEndianEncodingId, aStringEncodingId);
                    }
                    else if (aMessageType == REQUEST_MESSAGE)
                    {
                        aProtocolMessage = DecodeMessage(aReader, anEndianEncodingId, aStringEncodingId);
                    }
                    else
                    {
                        EneterTrace.Warning(TracedObject + "detected unknown string encoding.");
                        aProtocolMessage = null;
                    }

                    return aProtocolMessage;
                }
                catch (EndOfStreamException)
                {
                    // End of the stream.
                    return null;
                }
                catch (IOException)
                {
                    // End of the stream. (e.g. underlying socket was closed)
                    return null;
                }
                catch (ObjectDisposedException)
                {
                    // End of the stream.
                    return null;
                }
                catch (InvalidOperationException err)
                {
                    EneterTrace.Warning(TracedObject + "failed to decode the message.", err);

                    // Invalid message.
                    // Note: Just because somebody sends and invalid string the loop reading messages should
                    //       not be disturbed/interrupted by an exception.
                    //       The reading must continue with the next message.
                    return null;
                }
            }
        }

        /// <summary>
        /// Decodes a message or a response message from the byte[].
        /// </summary>
        /// <param name="readMessage">source of the low-level message</param>
        /// <returns></returns>
        public ProtocolMessage DecodeMessage(object readMessage)
        {
            using (EneterTrace.Entering())
            {
                using (MemoryStream aMemoryStream = new MemoryStream((byte[])readMessage))
                {
                    return DecodeMessage(aMemoryStream);
                }
            }
        }

        private void EncodeHeader(BinaryWriter writer)
        {
            //using (EneterTrace.Entering())
            //{
                // Header indicating the Eneter protocol with used Endian encoding.
                // Note: .NET uses Little Endian and UTF8
                byte[] aHeader = {(byte) 'E', (byte) 'N', (byte) 'E', (byte) 'T', (byte) 'E', (byte) 'R', LITTLE_ENDIAN, UTF8};
                writer.Write(aHeader);
            //}
        }

        private void EncodeMessage(BinaryWriter writer, object message)
        {
            //using (EneterTrace.Entering())
            //{
                if (message is String)
                {
                    writer.Write(STRING);
                    EncodeString(writer, (string)message);
                }
                else if (message is byte[])
                {
                    byte[] aBytes = (byte[])message;

                    writer.Write(BYTES);
                    writer.Write(aBytes.Length);
                    writer.Write(aBytes);
                }
                else
                {
                    string anErrorMessage = "The message is not serialized to string or byte[].";
                    EneterTrace.Error(anErrorMessage);
                    throw new InvalidOperationException(anErrorMessage);
                }
            //}
        }


        private ProtocolMessage DecodeRequest(EProtocolMessageType messageType, BinaryReader reader, int anEndianEncodingId, int aStringEncodingId)
        {
            //using (EneterTrace.Entering())
            //{
                string aResponseReceiverId = GetResponseReceiverId(reader, anEndianEncodingId, aStringEncodingId);

                ProtocolMessage aProtocolMessage = new ProtocolMessage(messageType, aResponseReceiverId, null);
                return aProtocolMessage;
            //}
        }

        private ProtocolMessage DecodeMessage(BinaryReader reader, int anEndianEncodingId, int aStringEncodingId)
        {
            //using (EneterTrace.Entering())
            //{
                string aResponseReceiverId = GetResponseReceiverId(reader, anEndianEncodingId, aStringEncodingId);
                object aMessage = GetMessage(reader, anEndianEncodingId, aStringEncodingId);

                ProtocolMessage aProtocolMessage = new ProtocolMessage(EProtocolMessageType.MessageReceived, aResponseReceiverId, aMessage);
                return aProtocolMessage;
            //}
        }


        private string GetResponseReceiverId(BinaryReader reader, int anEndianEncodingId, int aStringEncodingId)
        {
            //using (EneterTrace.Entering())
            //{
                int aSize = ReadInt(reader, anEndianEncodingId);

                byte[] aStringBytes = reader.ReadBytes(aSize);

                string aResponseReceiverId = DecodeString(aStringBytes, anEndianEncodingId, aStringEncodingId);

                return aResponseReceiverId;
            //}
        }

        private object GetMessage(BinaryReader reader, int anEndianEncodingId, int aStringEncodingId)
        {
            //using (EneterTrace.Entering())
            //{
                int aSerializationType = reader.ReadByte();
                int aSize = ReadInt(reader, anEndianEncodingId);

                byte[] aBytes = reader.ReadBytes(aSize);

                if (aSerializationType == BYTES)
                {
                    return aBytes;
                }

                if (aSerializationType == STRING)
                {
                    string aMessage = DecodeString(aBytes, anEndianEncodingId, aStringEncodingId);
                    return aMessage;
                }

                string anErrorMessage = "Received message is not serialized into byte[] or string.";
                EneterTrace.Error(anErrorMessage);
                throw new InvalidOperationException(anErrorMessage);
            //}
        }

        private int ReadInt(BinaryReader reader, int anEndianEncodingId)
        {
            //using (EneterTrace.Entering())
            //{
                int anInt = reader.ReadInt32();
            
                // Convert to little endian, if incoming data is in big endian.
                if (anEndianEncodingId == BIG_ENDIAN)
                {
                    anInt = ((anInt & 0x000000ff) << 24) + ((anInt & 0x0000ff00) << 8) +
                            ((anInt & 0x00ff0000) >> 8) +(int)((anInt & 0xff000000) >> 24);
                }
            
                return anInt;
            //}
        }


        private void EncodeString(BinaryWriter writer, string s)
        {
            //using (EneterTrace.Entering())
            //{
                byte[] aStringBytes = Encoding.UTF8.GetBytes(s);

                writer.Write(aStringBytes.Length);
                writer.Write(aStringBytes);
            //}
        }

        private String DecodeString(byte[] encodedString, int anEndianEncodingId, int aStringEncodingId)
        {
            //using (EneterTrace.Entering())
            //{
                Encoding anEncoding;

                if (aStringEncodingId == UTF16 && anEndianEncodingId == BIG_ENDIAN)
                {
                    anEncoding = Encoding.BigEndianUnicode;
                }
                else if (aStringEncodingId == UTF16 && anEndianEncodingId == LITTLE_ENDIAN)
                {
                    anEncoding = Encoding.Unicode;
                }
                else if (aStringEncodingId == UTF8)
                {
                    anEncoding = Encoding.UTF8;
                }
                else
                {
                    String anErrorMessage = TracedObject + "detected unknown string encoding.";
                    EneterTrace.Warning(anErrorMessage);
                    throw new InvalidOperationException(anErrorMessage);
                }

                String aResult = anEncoding.GetString(encodedString, 0, encodedString.Length);
                return aResult;
            //}
        }



        object IProtocolFormatter.EncodeOpenConnectionMessage(string responseReceiverId)
        {
            return EncodeOpenConnectionMessage(responseReceiverId);
        }


        // Type of encoding for numbers.
        private const byte LITTLE_ENDIAN = 10;
        private const byte BIG_ENDIAN = 20;

        // Type of encoding for strings.
        private const byte UTF8 = 10;
        private const byte UTF16 = 20;

        // Type of serialization.
        // E.g. If the message is serialized into XML, then it will indicate the type is string.
        //      If the message is serialized in a binary format, then it will indicate the type is byte[].
        private const byte BYTES = 10;
        private const byte STRING = 20;

        // Type of low level message - used for the low level communication between channels.
        private const byte OPEN_CONNECTION_REQUEST = 10;
        private const byte CLOSE_CONNECTION_REQUEST = 20;
        private const byte REQUEST_MESSAGE = 40;


        private string TracedObject
        {
            get
            {
                return GetType().Name + " ";
            }
        }
    }
}
