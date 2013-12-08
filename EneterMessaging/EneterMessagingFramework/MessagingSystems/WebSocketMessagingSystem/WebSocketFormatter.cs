/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/

#if !WINDOWS_PHONE_70

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.WebSocketMessagingSystem
{
    internal static class WebSocketFormatter
    {
        public static byte[] EncodeOpenConnectionHttpRequest(string pathAndQuery, IEnumerable<KeyValuePair<string, string>> headerFields)
        {
            using (EneterTrace.Entering())
            {
                StringBuilder anHttpRequestBuilder = new StringBuilder();
                anHttpRequestBuilder.AppendFormat(CultureInfo.InvariantCulture, "GET {0} HTTP/1.1\r\n", pathAndQuery);

                foreach (KeyValuePair<string, string> aHeaderField in headerFields)
                {
                    anHttpRequestBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0}: {1}\r\n", aHeaderField.Key, aHeaderField.Value);
                }

                anHttpRequestBuilder.Append("\r\n");

                string anHttpRequest = anHttpRequestBuilder.ToString();

                return Encoding.UTF8.GetBytes(anHttpRequest);
            }
        }

#if !SILVERLIGHT
        public static byte[] EncodeOpenConnectionHttpResponse(string webSocketKey)
        {
            using (EneterTrace.Entering())
            {
                // Create response key.
                string aResponseKey = EncryptWebSocketKey(webSocketKey);

                // Create the response message.
                string anHttpResponse = string.Format(CultureInfo.InvariantCulture, "HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Accept: {0}\r\n\r\n", aResponseKey);

                // Convert response to bytes.
                byte[] aMessageBytes = Encoding.UTF8.GetBytes(anHttpResponse);

                return aMessageBytes;
            }
        }
#endif

        public static byte[] EncodeBinaryMessageFrame(bool isFinal, byte[] maskingKey, byte[] message)
        {
            using (EneterTrace.Entering())
            {
                return EncodeMessage(isFinal, 0x02, maskingKey, message);
            }
        }

        public static byte[] EncodeTextMessageFrame(bool isFinal, byte[] maskingKey, string message)
        {
            using (EneterTrace.Entering())
            {
                byte[] aTextMessage = Encoding.UTF8.GetBytes(message);
                return EncodeMessage(isFinal, 0x01, maskingKey, aTextMessage);
            }
        }

        public static byte[] EncodeContinuationMessageFrame(bool isFinal, byte[] maskingKey, string message)
        {
            using (EneterTrace.Entering())
            {
                byte[] aTextMessage = Encoding.UTF8.GetBytes(message);
                return EncodeMessage(isFinal, 0x00, maskingKey, aTextMessage);
            }
        }

        public static byte[] EncodeContinuationMessageFrame(bool isFinal, byte[] maskingKey, byte[] message)
        {
            using (EneterTrace.Entering())
            {
                return EncodeMessage(isFinal, 0x00, maskingKey, message);
            }
        }

        public static byte[] EncodeCloseFrame(byte[] maskingKey, ushort statusCode)
        {
            using (EneterTrace.Entering())
            {
                // Convert to little endian.
                ushort aStatusCode = ConvertEndian(statusCode);

                return EncodeMessage(true, 0x08, maskingKey, BitConverter.GetBytes(aStatusCode));
            }
        }

        public static byte[] EncodePingFrame(byte[] maskingKey)
        {
            using (EneterTrace.Entering())
            {
                // Note: If not data is added then ping has only 2 bytes and the message can stay in a computer buffer
                //       for a very long time. So add some dummy data to the ping message. (websocket protocol supports that)
                byte[] aDummy = { (byte)'H', (byte)'e', (byte)'l', (byte)'l', (byte)'o' };
                return EncodeMessage(true, 0x09, maskingKey, aDummy);
            }
        }

        public static byte[] EncodePongFrame(byte[] maskingKey, byte[] pongData)
        {
            using (EneterTrace.Entering())
            {
                return EncodeMessage(true, 0x0A, maskingKey, pongData);
            }
        }

        public static byte[] EncodeMessage(bool isFinal, byte opCode, byte[] maskingKey, byte[] payload)
        {
            using (EneterTrace.Entering())
            {
                if (maskingKey != null && maskingKey.Length != 4)
                {
                    throw new ArgumentException("The input parameter maskingKey must be null or must have length 4.");
                }

                using (MemoryStream aBuffer = new MemoryStream())
                {
                    using (BinaryWriter aWriter = new BinaryWriter(aBuffer))
                    {
                        byte aFirstByte = (byte)(((isFinal) ? 0x80 : 0x00) | opCode);
                        aWriter.Write(aFirstByte);

                        byte aLengthIndicator = (byte)((payload == null) ? 0 : (payload.Length <= 125) ? payload.Length : (payload.Length <= 0xFFFF) ? 126 : 127);

                        byte aSecondByte = (byte)(((maskingKey != null) ? 0x80 : 0x00) | aLengthIndicator);
                        aWriter.Write(aSecondByte);

                        if (aLengthIndicator == 126)
                        {
                            // Convert the length to big endian.
                            ushort aLenth = (ushort)payload.Length;
                            aLenth = ConvertEndian(aLenth);

                            aWriter.Write(aLenth);
                        }
                        else if (aLengthIndicator == 127)
                        {
                            ulong aLength = ConvertEndian((ulong)payload.Length);
                            aWriter.Write(aLength);
                        }

                        if (maskingKey != null)
                        {
                            aWriter.Write(maskingKey);
                        }

                        if (payload != null)
                        {
                            if (maskingKey != null)
                            {
                                for (int i = 0; i < payload.Length; ++i)
                                {
                                    aWriter.Write((byte)(payload[i] ^ maskingKey[i % 4]));
                                }
                            }
                            else
                            {
                                aWriter.Write(payload);
                            }
                        }

                        return aBuffer.ToArray();
                    }
                }
            }
        }

#if !SILVERLIGHT
        public static Match DecodeOpenConnectionHttpRequest(Stream inputStream)
        {
            using (EneterTrace.Entering())
            {
                // Read HTTP header.
                string anHttpHeaderStr = ReadHttp(inputStream);

                // Parse HTTP header.
                Match aMatch = myHttpOpenConnectionRequest.Match(anHttpHeaderStr);

                return aMatch;
            }
        }
#endif

        public static Match DecodeOpenConnectionHttpResponse(Stream inputStream)
        {
            using (EneterTrace.Entering())
            {
                // Read HTTP header.
                string anHttpHeaderStr = ReadHttp(inputStream);

                // Parse HTTP header.
                Match aMatch = myHttpOpenConnectionResponse.Match(anHttpHeaderStr);

                return aMatch;
            }
        }

        public static IDictionary<string, string> GetHttpHeaderFields(Match http)
        {
            using (EneterTrace.Entering())
            {
                Dictionary<string, string> aHeaderFields = new Dictionary<string, string>();

                // Get WebSocket request.
                CaptureCollection aHeaderKeys = http.Groups["headerKey"].Captures;
                CaptureCollection aHeaderValues = http.Groups["headerValue"].Captures;
                for (int i = 0; i < aHeaderKeys.Count; ++i)
                {
                    string aHeaderKey = aHeaderKeys[i].Value;
                    string aHeaderValue = aHeaderValues[i].Value;

                    aHeaderFields.Add(aHeaderKey, aHeaderValue);
                }

                return aHeaderFields;
            }
        }

        public static WebSocketFrame DecodeFrame(Stream inputStream)
        {
            using (EneterTrace.Entering())
            {
                try
                {
                    // Note: Do not enclose the BinaryReader with using because it will close the stream!!!
                    BinaryReader aReader = new BinaryReader(inputStream);

                    // Read the first 2 bytes.
                    byte[] aFirst2Bytes = aReader.ReadBytes(2);
                    if (aFirst2Bytes.Length < 2)
                    {
                        throw new EndOfStreamException("End of stream during reading first two bytes of web socket frame.");
                    }

                    // Get if final.
                    bool anIsFinal = (aFirst2Bytes[0] & 0x80) != 0;

                    // Get opcode.
                    EFrameType aFrameType = (EFrameType)(aFirst2Bytes[0] & 0xF);

                    // Get if masked.
                    bool anIsMasked = (aFirst2Bytes[1] & 0x80) != 0;

                    // Get the message length.
                    int aMessageLength = aFirst2Bytes[1] & 0x7F;
                    if (aMessageLength == 126)
                    {
                        // The length is encoded in next 2 bytes (16 bits).
                        ushort aLength = aReader.ReadUInt16();

                        // Websockets are in big endians, so convert it to little endian.
                        aMessageLength = ConvertEndian(aLength);
                    }
                    else
                    if (aMessageLength == 127)
                    {
                        // The length is encoded in next 8 bytes (64 bits).
                        ulong aLength = aReader.ReadUInt64();

                        // Websockets are in big endians, so convert it to little endian.
                        aLength = ConvertEndian(aLength);

                        aMessageLength = (int)aLength;
                    }

                    // Get mask bytes.
                    byte[] aMaskBytes = null;
                    if (anIsMasked)
                    {
                        aMaskBytes = aReader.ReadBytes(4);

                        if (aMaskBytes.Length < 4)
                        {
                            throw new EndOfStreamException("End of stream during reading web socket mask bytes.");
                        }
                    }

                    // Get the message data.
                    byte[] aMessageData = aReader.ReadBytes(aMessageLength);
                    if (aMessageData.Length < aMessageLength)
                    {
                        throw new EndOfStreamException("End of stream during reading message data from the web socket frame.");
                    }

                    // If mask was used then unmask data.
                    if (anIsMasked)
                    {
                        for (int i = 0; i < aMessageData.Length; ++i)
                        {
                            aMessageData[i] = (byte)(aMessageData[i] ^ aMaskBytes[i % 4]);
                        }
                    }

                    WebSocketFrame aFrame = new WebSocketFrame(aFrameType, anIsMasked, aMessageData, anIsFinal);

                    return aFrame;
                }
                catch (EndOfStreamException)
                {
                    // End of the stream.
                    return null;
                }
                catch (ObjectDisposedException)
                {
                    // End of the stream.
                    return null;
                }
            }
        }

        public static string EncryptWebSocketKey(string webSocketKeyBase64)
        {
            string anEncryptedKey = webSocketKeyBase64 + myWebSocketId;

#if !SILVERLIGHT
            byte[] anEncodedResponseKey = SHA1.Create().ComputeHash(Encoding.ASCII.GetBytes(anEncryptedKey));
#else
            SHA1 aSHA1 = new SHA1Managed();
            byte[] anEncodedResponseKey = aSHA1.ComputeHash(Encoding.UTF8.GetBytes(anEncryptedKey));
#endif
            string anEncryptedKeyBase64 = Convert.ToBase64String(anEncodedResponseKey);

            return anEncryptedKeyBase64;
        }

        private static ushort ConvertEndian(ushort num)
        {
            ushort aResult = (ushort)(((num & 0x00FF) << 8) + ((num & 0xFF00) >> 8));
            return aResult;
        }

        private static ulong ConvertEndian(ulong num)
        {
            ulong aResult = ((num & 0x00000000000000FF) << 56) +
                            ((num & 0x000000000000FF00) << 40) +
                            ((num & 0x0000000000FF0000) << 24) +
                            ((num & 0x00000000FF000000) << 8) +
                            ((num & 0x000000FF00000000) >> 8) +
                            ((num & 0x0000FF0000000000) >> 24) +
                            ((num & 0x00FF000000000000) >> 40) +
                            ((num & 0xFF00000000000000) >> 56);
            return aResult;
        }

        private static string ReadHttp(Stream inputStream)
        {
            using (EneterTrace.Entering())
            {
                // Read HTTP header.
                using (MemoryStream anHttpHeaderBuffer = new MemoryStream())
                {
                    int aStopFlag = 4;
                    while (aStopFlag != 0)
                    {
                        int aValue = inputStream.ReadByte();
                        if (aValue == -1)
                        {
                            string anErrorMessage = "End of stream during reading HTTP header.";
                            EneterTrace.Error(anErrorMessage);
                            throw new InvalidOperationException(anErrorMessage);
                        }

                        // Store the received byte - belonging to the header.
                        anHttpHeaderBuffer.WriteByte((byte)aValue);

                        // Detect sequence /r/n/r/n - the end of the HTTP header.
                        if (aValue == 13 && (aStopFlag == 4 || aStopFlag == 2) ||
                           (aValue == 10 && (aStopFlag == 3 || aStopFlag == 1)))
                        {
                            --aStopFlag;
                        }
                        else
                        {
                            aStopFlag = 4;
                        }
                    }

                    byte[] anHttpHeaderBytes = anHttpHeaderBuffer.ToArray();
                    string anHttpHeaderStr = Encoding.UTF8.GetString(anHttpHeaderBytes, 0, anHttpHeaderBytes.Length);
                    return anHttpHeaderStr;
                }
            }
        }

#if !SILVERLIGHT
        private static readonly Regex myHttpOpenConnectionRequest = new Regex(
                           @"^GET\s(?<path>[^\s\?]+)(?<query>\?[^\s]+)?\sHTTP\/1\.1\r\n" +
                           @"((?<headerKey>[^:\r\n]+):\s(?<headerValue>[^\r\n]+)\r\n)+" +
                           @"\r\n",
                           RegexOptions.IgnoreCase);
#endif

        private static readonly Regex myHttpOpenConnectionResponse = new Regex(
                           @"HTTP/1\.1\s(?<code>[\d]+)\s.*\r\n" +
                           @"((?<headerKey>[^:\r\n]+):\s(?<headerValue>[^\r\n]+)\r\n)+" +
                           @"\r\n",
                           RegexOptions.IgnoreCase);

        private const string myWebSocketId = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
    }
}


#endif