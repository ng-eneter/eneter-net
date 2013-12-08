/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/


#if COMPACT_FRAMEWORK

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.HttpMessagingSystem
{
    internal static class HttpFormatter
    {
        static HttpFormatter()
        {
            myReasonPhrases[200] = "Continue";
            myReasonPhrases[400] = "Bad Request";
            myReasonPhrases[404] = "Not Found";
        }

        public static Match DecodeHttpRequest(Stream inputStream)
        {
            using (EneterTrace.Entering())
            {
                // Read HTTP header.
                // Note: 2 clrf means \r\n\r\n
                string anHttpHeaderStr = ReadHttp(inputStream, 2);

                // Parse HTTP header.
                Match aMatch = myHttpRequestRegex.Match(anHttpHeaderStr);

                return aMatch;
            }
        }

        public static byte[] DecodeChunk(Stream inputStream)
        {
            using (EneterTrace.Entering())
            {
                // Read HTTP chunk line.
                // Note: 1 clrf means \r\n
                string aChunkLine = ReadHttp(inputStream, 1);

                Match aMatch = myHttpChunkRegex.Match(aChunkLine);
                if (!aMatch.Success)
                {
                    throw new InvalidOperationException("Incorrect format of line identifying the size of the http chunk.");
                }

                // Extract size of the chunk.
                string aSizeHex = aMatch.Groups["size"].Value;
                int aChunkSize = int.Parse(aSizeHex, NumberStyles.HexNumber);

                // Read the chunk.
                // Note: Do not enclose the BinaryReader with using because it will close the stream!!!
                BinaryReader aReader = new BinaryReader(inputStream);
                byte[] aChunkData = aReader.ReadBytes(aChunkSize);
                
                // Chunk data is folowed by \r\n - so read it to position the stream behind this chunk.
                byte[] aFooter = aReader.ReadBytes(2);

                if (aFooter[0] != (byte)'\r' && aFooter[1] != (byte)'\n')
                {
                    throw new InvalidOperationException("Http chunk data is not followed by \\r\\n.");
                }

                return aChunkData;
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

        public static byte[] EncodeError(int errorCode)
        {
            using (EneterTrace.Entering())
            {
                return EncodeResponse(errorCode, null, false);
            }
        }

        public static byte[] EncodeResponse(int statusCode, byte[] responseData, bool isChunk)
        {
            using (EneterTrace.Entering())
            {
                // Response status line.
                StringBuilder aHeaderBuilder = new StringBuilder();
                aHeaderBuilder.AppendFormat(CultureInfo.InvariantCulture, "HTTP/1.1 {0} {1}\r\n", statusCode, myReasonPhrases[statusCode]);

                // If response data is present and can be sent.
                bool isData = statusCode >= 200 && statusCode != 204 && statusCode != 304 &&
                              responseData != null && responseData.Length > 0;

                Dictionary<string, string> aHeaderFields = new Dictionary<string, string>();
                aHeaderFields["Content-Type"] = "application/octet-stream";

                if (isData)
                {
                    // If it is not the chunk.
                    if (!isChunk)
                    {
                        aHeaderFields["Content-Length"] = responseData.Length.ToString();
                    }
                    else
                    {
                        // Ensure 'Content-Length' is not among header fields.
                        aHeaderFields.Remove("Content-Length");

                        // Ensure 'Transfer-encoding: chunked'
                        aHeaderFields["Transfer-encoding"] = "chunked";
                    }
                }

                // Add header fields.
                foreach (KeyValuePair<string, string> aHeaderField in aHeaderFields)
                {
                    aHeaderBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0}: {1}\r\n", aHeaderField.Key, aHeaderField.Value);
                }

                // End of http header.
                aHeaderBuilder.Append("\r\n");
                
                // Convert http header to bytes.
                string anHttpHeader = aHeaderBuilder.ToString();
                byte[] aHeaderBytes = Encoding.UTF8.GetBytes(anHttpHeader);

                // If response data is available.
                if (isData)
                {
                    byte[] aMessageBytes = new byte[aHeaderBytes.Length + responseData.Length];
                    Buffer.BlockCopy(aHeaderBytes, 0, aMessageBytes, 0, aHeaderBytes.Length);
                    Buffer.BlockCopy(responseData, 0, aMessageBytes, aHeaderBytes.Length, responseData.Length);

                    return aMessageBytes;
                }

                return aHeaderBytes;
            }
        }

        // This is not needed for now.
        /*
        public static byte[] EncodeNextChunk(byte[] responseMessage)
        {
            using (EneterTrace.Entering())
            {
                int aChunkSize = (responseMessage != null) ? responseMessage.Length : 0;
                string aChunk = aChunkSize.ToString("X") + "\r\n";

                // if it is the last chunk.
                if (aChunkSize == 0)
                {
                    aChunk += "\r\n";

                    // Convert to bytes.
                    byte[] aChunkBytes = Encoding.UTF8.GetBytes(aChunk);

                    return aChunkBytes;
                }
                else
                {
                    byte[] aFirstLine = Encoding.UTF8.GetBytes(aChunk);

                    byte[] aChunkBytes = new byte[aFirstLine.Length + responseMessage.Length + 2];
                    Buffer.BlockCopy(aFirstLine, 0, aChunkBytes, 0, aFirstLine.Length);
                    Buffer.BlockCopy(responseMessage, 0, aChunkBytes, aFirstLine.Length, responseMessage.Length);
                    aChunkBytes[aChunkBytes.Length - 2] = (byte)'\r';
                    aChunkBytes[aChunkBytes.Length - 1] = (byte)'\n';

                    return aChunkBytes;
                }
            }
        }
        */

        /// <summary>
        /// Reads the http part from the stream.
        /// </summary>
        /// <remarks>
        /// \r\n means 1 clrf
        /// \r\n\r\n means 2 clrf
        /// </remarks>
        /// <param name="inputStream"></param>
        /// <param name="numberOfClrf"></param>
        /// <returns></returns>
        private static string ReadHttp(Stream inputStream, int numberOfClrf)
        {
            using (EneterTrace.Entering())
            {
                // Read HTTP header.
                using (MemoryStream anHttpHeaderBuffer = new MemoryStream())
                {
                    int aStopFlag = numberOfClrf * 2;
                    while (aStopFlag != 0)
                    {
                        int aValue = inputStream.ReadByte();
                        if (aValue == -1)
                        {
                            string anErrorMessage = "End of stream during reading HTTP header.";
                            EneterTrace.Error(anErrorMessage);
                            throw new EndOfStreamException(anErrorMessage);
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
                            aStopFlag = numberOfClrf * 2;
                        }
                    }

                    byte[] anHttpHeaderBytes = anHttpHeaderBuffer.ToArray();
                    string anHttpHeaderStr = Encoding.UTF8.GetString(anHttpHeaderBytes, 0, anHttpHeaderBytes.Length);
                    return anHttpHeaderStr;
                }
            }
        }

        private static readonly Regex myHttpRequestRegex = new Regex(
                           @"^(?<method>[A-Za-z]+)\s(?<path>[^\s\?]+)(?<query>\?[^\s]+)?\sHTTP\/1\.1\r\n" +
                           @"((?<headerKey>[^:\r\n]+):\s(?<headerValue>[^\r\n]+)\r\n)+" +
                           @"\r\n",
                           RegexOptions.IgnoreCase);

        private static readonly Regex myHttpChunkRegex = new Regex(
                           @"^(?<size>[\w]+)\s[.]?\r\n",
                           RegexOptions.IgnoreCase);

        private static Dictionary<int, string> myReasonPhrases = new Dictionary<int, string>();
    }
}


#endif