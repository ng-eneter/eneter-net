/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

//#if !COMPACT_FRAMEWORK20

using System;
using System.IO;
using System.Text;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.DataProcessing.Serializing
{
    internal class EncoderDecoder
    {
        public void Encode(Stream writer, object dataToEncode)
        {
            using (EneterTrace.Entering())
            {
                // Encrypt serialized data.
                if (dataToEncode is string)
                {
                    // Note: UTF-8 is used by .NET
                    byte[] aDataToEncode = Encoding.UTF8.GetBytes((string)dataToEncode);

                    // Write info that encoded data is UTF8.
                    writer.WriteByte(STRING_UTF8_ID);

                    // Encode data.
                    writer.Write(aDataToEncode, 0, aDataToEncode.Length);
                }
                else if (dataToEncode is byte[])
                {
                    // Write info, that encoded data is array of bytes.
                    writer.WriteByte(BYTES_ID);

                    // Encode data.
                    writer.Write((byte[])dataToEncode, 0, ((byte[])dataToEncode).Length);
                }
                else
                {
                    String anErrorMessage = "Encoding of data failed because the underlying serializer does not serialize to String or byte[].";
                    EneterTrace.Error(anErrorMessage);
                    throw new InvalidOperationException(anErrorMessage);
                }
            }
        }

        public object Decode(Stream reader)
        {
            using (EneterTrace.Entering())
            {
                Object aDecodedData = null;

                // Read type of data.
                int aDataType = reader.ReadByte();
                if (aDataType == -1)
                {
                    String anErrorMessage = "Decoding of serialized data failed because of unexpected end of stream.";
                    EneterTrace.Error(anErrorMessage);
                    throw new InvalidOperationException(anErrorMessage);
                }
                if (aDataType != STRING_UTF8_ID &&
                    aDataType != STRING_UTF16_LE_ID &&
                    aDataType != STRING_UTF16_BE_ID &&
                    aDataType != BYTES_ID)
                {
                    String anErrorMessage = "Decoding of serialized data failed because of incorrect data fromat.";
                    EneterTrace.Error(anErrorMessage);
                    throw new InvalidOperationException(anErrorMessage);
                }

                // Read bytes from the stream until the end.
                byte[] aDecodedBytes;
                using (MemoryStream aDecodedBytesStream = new MemoryStream())
                {
                    byte[] aBuffer = new byte[32000];
                    int aSize;
                    while ((aSize = reader.Read(aBuffer, 0, aBuffer.Length)) > 0)
                    {
                        aDecodedBytesStream.Write(aBuffer, 0, aSize);
                    }
                    aDecodedBytes = aDecodedBytesStream.ToArray();
                }

                if (aDataType == STRING_UTF8_ID)
                {
                    Encoding anEncoding = Encoding.UTF8;
                    aDecodedData = anEncoding.GetString(aDecodedBytes, 0, aDecodedBytes.Length);
                }
                else if (aDataType == STRING_UTF16_LE_ID)
                {
                    Encoding anEncoding = Encoding.Unicode;
                    aDecodedData = anEncoding.GetString(aDecodedBytes, 0, aDecodedBytes.Length);
                }
                else if (aDataType == STRING_UTF16_BE_ID)
                {
                    Encoding anEncoding = Encoding.BigEndianUnicode;
                    aDecodedData = anEncoding.GetString(aDecodedBytes, 0, aDecodedBytes.Length);
                }
                else if (aDataType == BYTES_ID)
                {
                    aDecodedData = aDecodedBytes;
                }
                else
                {
                    String anErrorMessage = "Decoding of serialized data failed because of incorrect data fromat.";
                    EneterTrace.Error(anErrorMessage);
                    throw new InvalidOperationException(anErrorMessage);
                }

                return aDecodedData;
            }
        }

        private readonly byte STRING_UTF8_ID = 10;
        private readonly byte STRING_UTF16_LE_ID = 20;
        private readonly byte STRING_UTF16_BE_ID = 30;
        private readonly byte BYTES_ID = 40;
    }
}

//#endif