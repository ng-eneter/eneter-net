﻿/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/

using System;
using System.IO;
using Eneter.Messaging.DataProcessing.Serializing;

namespace Eneter.Messaging.MessagingSystems.ConnectionProtocols
{
    public class InteroperableProtocolFormatter : IProtocolFormatter
    {
        public InteroperableProtocolFormatter()
            : this(true)
        {
        }

        public InteroperableProtocolFormatter(bool isLittleEndian)
        {
            myIsLittleEndian = isLittleEndian;
        }

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

                    myEncoderDecoder.WritePlainByteArray(aWriter, aMessageData, myIsLittleEndian);
                }
            //}
        }

        public ProtocolMessage DecodeMessage(object readMessage)
        {
            using (MemoryStream aMemoryStream = new MemoryStream((byte[])readMessage))
            {
                return DecodeMessage((Stream)aMemoryStream);
            }
        }

        public ProtocolMessage DecodeMessage(Stream readStream)
        {
            try
            {
                BinaryReader aReader = new BinaryReader(readStream);
                byte[] aMessageData = myEncoderDecoder.ReadPlainByteArray(aReader, myIsLittleEndian);

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

        private bool myIsLittleEndian;
        private EncoderDecoder myEncoderDecoder = new EncoderDecoder();
    }
}
