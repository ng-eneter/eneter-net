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



        public void Write(BinaryWriter writer, object data, bool isLittleEndianRequested)
        {
            if (data is string)
            {
                WriteString(writer, (string)data, isLittleEndianRequested);
            }
            else if (data is byte[])
            {
                WriteByteArray(writer, (byte[])data, isLittleEndianRequested);
            }
            else
            {
                throw new InvalidOperationException("Only byte[] or string is supported.");
            }
        }

        public void WriteString(BinaryWriter writer, string data, bool isLittleEndianRequested)
        {
            // Write info, that encoded data is string.
            writer.Write((byte)STRING_UTF8_ID);
            WritePlainString(writer, data, Encoding.UTF8, isLittleEndianRequested);
        }

        public void WriteByteArray(BinaryWriter writer, byte[] data, bool isLittleEndianRequested)
        {
            // Write info, that encoded data is array of bytes.
            writer.Write((byte)BYTES_ID);
            WritePlainByteArray(writer, data, isLittleEndianRequested);
        }

        public object Read(BinaryReader reader, bool isLittleEndian)
        {
            byte aDataType = reader.ReadByte();
            object aResult;

            if (aDataType == BYTES_ID)
            {
                aResult = ReadPlainByteArray(reader, isLittleEndian);
            }
            else
            {
                Encoding anEncoding = null;

                if (aDataType == STRING_UTF8_ID)
                {
                    anEncoding = Encoding.UTF8;
                }
                else if (aDataType == STRING_UTF16_LE_ID)
                {
                    anEncoding = Encoding.Unicode;
                }
                else if (aDataType == STRING_UTF16_BE_ID)
                {
                    anEncoding = Encoding.BigEndianUnicode;
                }

                aResult = ReadPlainString(reader, anEncoding, isLittleEndian);
            }

            return aResult;
        }

        public void WritePlainString(BinaryWriter writer, string data, Encoding stringEncoding, bool isLittleEndianRequested)
        {
            byte[] aDataBytes = stringEncoding.GetBytes(data);

            WritePlainByteArray(writer, aDataBytes, isLittleEndianRequested);
        }

        public string ReadPlainString(BinaryReader reader, Encoding stringEncoding, bool isLittleEndian)
        {
            byte[] aStringBytes = ReadPlainByteArray(reader, isLittleEndian);

            string aResult = stringEncoding.GetString(aStringBytes, 0, aStringBytes.Length);
            return aResult;
        }

        public void WritePlainByteArray(BinaryWriter writer, byte[] data, bool isLittleEndianRequested)
        {
            // Length of the array.
            WriteInt32(writer, data.Length, isLittleEndianRequested);

            // Bytes.
            writer.Write(data);
        }

        public byte[] ReadPlainByteArray(BinaryReader reader, bool isLittleEndian)
        {
            int aLength = ReadInt32(reader, isLittleEndian);
            byte[] aData = reader.ReadBytes(aLength);

            return aData;
        }

        public void WriteInt32(BinaryWriter writer, int value, bool isLittleEndianRequested)
        {
            // If the endianess of the machine is different than requested endianess then correct it.
            if (BitConverter.IsLittleEndian != isLittleEndianRequested)
            {
                value = SwitchEndianess(value);
            }

            writer.Write((int)value);
        }

        public int ReadInt32(BinaryReader reader, bool isLittleEndian)
        {
            int aValue = reader.ReadInt32();

            if (BitConverter.IsLittleEndian != isLittleEndian)
            {
                // If the endianess of the machine is same as requested endianess then just write.
                aValue = SwitchEndianess(aValue);
            }

            return aValue;
        }


        private int SwitchEndianess(int i)
        {
            int anInt = ((i & 0x000000ff) << 24) + ((i & 0x0000ff00) << 8) +
                        ((i & 0x00ff0000) >> 8) + (int)((i & 0xff000000) >> 24);

            return anInt;
        }


        private readonly byte STRING_UTF8_ID = 10;
        private readonly byte STRING_UTF16_LE_ID = 20;
        private readonly byte STRING_UTF16_BE_ID = 30;
        private readonly byte BYTES_ID = 40;
    }
}

//#endif