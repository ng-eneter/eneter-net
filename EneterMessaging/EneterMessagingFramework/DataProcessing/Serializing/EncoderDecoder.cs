/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

#if !COMPACT_FRAMEWORK20

using System;
using System.IO;
using System.Text;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.DataProcessing.Serializing
{
    internal class EncoderDecoder
    {
        public EncoderDecoder(ISerializer underlyingSerializer)
        {
            using (EneterTrace.Entering())
            {
                myUnderlyingSerializer = underlyingSerializer;
            }
        }

        public void Serialize<T>(Stream writer, T dataToSerialize)
        {
            using (EneterTrace.Entering())
            {
                // Use underlying serializer to serialize data.
                Object aSerializedData = myUnderlyingSerializer.Serialize<T>(dataToSerialize);

                // Encrypt serialized data.
                if (aSerializedData is string)
                {
                    // Note: UTF-8 is used by .NET
                    byte[] aDataToEncode = Encoding.UTF8.GetBytes((string)aSerializedData);

                    // Write info that encoded data is UTF8.
                    writer.WriteByte(STRING_UTF8_ID);

                    // Encode data.
                    writer.Write(aDataToEncode, 0, aDataToEncode.Length);
                }
                else if (aSerializedData is byte[])
                {
                    // Write info, that encoded data is array of bytes.
                    writer.WriteByte(BYTES_ID);

                    // Encode data.
                    writer.Write((byte[])aSerializedData, 0, ((byte[])aSerializedData).Length);
                }
                else
                {
                    String anErrorMessage = "Encoding of data failed because the underlying serializer does not serialize to String or byte[].";
                    EneterTrace.Error(anErrorMessage);
                    throw new InvalidOperationException(anErrorMessage);
                }
            }
        }

        public T Deserialize<T>(Stream reader)
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

                // Use underlying serializer to deserialize data.
                return myUnderlyingSerializer.Deserialize<T>(aDecodedData);
            }
        }

        private ISerializer myUnderlyingSerializer;
    
        private readonly byte STRING_UTF8_ID = 10;
        private readonly byte STRING_UTF16_LE_ID = 20;
        private readonly byte STRING_UTF16_BE_ID = 30;
        private readonly byte BYTES_ID = 40;
    }
}

#endif