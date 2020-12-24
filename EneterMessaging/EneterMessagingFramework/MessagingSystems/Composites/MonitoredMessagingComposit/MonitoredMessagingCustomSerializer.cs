

using System;
using System.IO;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.MessagingSystems.Composites.MonitoredMessagingComposit
{
    internal class MonitoredMessagingCustomSerializer : ISerializer
    {
        public object Serialize<T>(T dataToSerialize)
        {
            using (EneterTrace.Entering())
            {
                if (typeof(T) != typeof(MonitorChannelMessage))
                {
                    throw new InvalidOperationException("Only " + typeof(MonitorChannelMessage).Name + " can be serialized.");
                }

                object aTemp = dataToSerialize;
                MonitorChannelMessage aMessage = (MonitorChannelMessage)aTemp;

                using (MemoryStream aStream = new MemoryStream())
                {
                    BinaryWriter aWriter = new BinaryWriter(aStream);

                    // Write message type.
                    byte aMessageType = (byte)aMessage.MessageType;
                    aWriter.Write((byte)aMessageType);

                    // Write message data.
                    if (aMessage.MessageType == MonitorChannelMessageType.Message)
                    {
                        if (aMessage.MessageContent == null)
                        {
                            throw new InvalidOperationException("Message data is null.");
                        }

                        myEncoderDecoder.Write(aWriter, aMessage.MessageContent, myIsLittleEndian);
                    }

                    return aStream.ToArray();
                }
            }
        }

        public T Deserialize<T>(object serializedData)
        {
            using (EneterTrace.Entering())
            {
                if (serializedData is byte[] == false)
                {
                    throw new ArgumentException("Input parameter 'serializedData' is not byte[].");
                }

                if (typeof(T) != typeof(MonitorChannelMessage))
                {
                    throw new InvalidOperationException("Data can be deserialized only into" + typeof(MonitorChannelMessage).Name);
                }

                MonitorChannelMessage aResult;

                byte[] aData = (byte[])serializedData;

                using (MemoryStream aStream = new MemoryStream(aData))
                {
                    BinaryReader aReader = new BinaryReader(aStream);

                    // Read type of the message.
                    int aRequest = aReader.ReadByte();
                    MonitorChannelMessageType aMessageType = (MonitorChannelMessageType)aRequest;

                    // If it is the message then read data.
                    object aMessageData = null;
                    if (aMessageType == MonitorChannelMessageType.Message)
                    {
                        aMessageData = myEncoderDecoder.Read(aReader, myIsLittleEndian);
                    }

                    aResult = new MonitorChannelMessage(aMessageType, aMessageData);
                    return (T)(object)aResult;
                }
            }
        }

        private bool myIsLittleEndian = true;
        private EncoderDecoder myEncoderDecoder = new EncoderDecoder();
    }
}
