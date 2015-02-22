/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/

using System;
using System.IO;

namespace Eneter.Messaging.MessagingSystems.ConnectionProtocols
{
    public class InteroperableProtocolFormatter : IProtocolFormatter
    {

        public object EncodeOpenConnectionMessage(string responseReceiverId)
        {
            // An explicit open connection message is not supported.
            return null;
        }

        public void EncodeOpenConnectionMessage(string responseReceiverId, Stream outputSream)
        {
            // An explicit open connection message is not supported therefore write nothing into the sream.
        }

        public object EncodeCloseConnectionMessage(string responseReceiverId)
        {
            // An explicit close connection message is not supported.
            return null;
        }

        public void EncodeCloseConnectionMessage(string responseReceiverId, Stream outputSream)
        {
            // // An explicit close connection message is not supported therefore write nothing into the stream.
        }

        public object EncodeMessage(string responseReceiverId, object message)
        {
            //using (EneterTrace.Entering())
            //{
                using (MemoryStream aBuffer = new MemoryStream())
                {
                    EncodeMessage(responseReceiverId, message, aBuffer);
                    return aBuffer.ToArray();
                }
            //}
        }

        public void EncodeMessage(string responseReceiverId, object message, Stream outputSream)
        {
            //using (EneterTrace.Entering())
            //{
                byte[] aMessageData = (byte[])message;

                if (aMessageData.Length > 0)
                {
                    BinaryWriter aWriter = new BinaryWriter(outputSream);

                    // Convert size to Big Endian.
                    int aBigEndianSize = ChangeEndianess(aMessageData.Length);

                    // Write size of the message.
                    aWriter.Write(aBigEndianSize);

                    // Write the message.
                    aWriter.Write(aMessageData, 0, aMessageData.Length);
                }
            //}
        }

        public ProtocolMessage DecodeMessage(Stream readStream)
        {
            try
            {
                BinaryReader aReader = new BinaryReader(readStream);

                // Read the size.
                int aBigEndianSize = aReader.ReadInt32();
                int aLittleEndianSize = ChangeEndianess(aBigEndianSize);

                byte[] aMessageData = aReader.ReadBytes(aLittleEndianSize);

                ProtocolMessage aProtocolMessage = new ProtocolMessage(EProtocolMessageType.MessageReceived, "", aMessageData);
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
        }

        public ProtocolMessage DecodeMessage(object readMessage)
        {
            using (MemoryStream aMemoryStream = new MemoryStream((byte[])readMessage))
            {
                return DecodeMessage(aMemoryStream);
            }
        }

        private int ChangeEndianess(int i)
        {
            int anInt = ((i & 0x000000ff) << 24) + ((i & 0x0000ff00) << 8) +
                        ((i & 0x00ff0000) >> 8) + (int)((i & 0xff000000) >> 24);

            return anInt;
        }

    }
}
