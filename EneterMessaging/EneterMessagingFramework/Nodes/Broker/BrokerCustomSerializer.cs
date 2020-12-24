

using System;
using System.IO;
using System.Text;
using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.Nodes.Broker
{
    internal class BrokerCustomSerializer : ISerializer
    {
        public BrokerCustomSerializer()
            : this(true)
        {
        }

        public BrokerCustomSerializer(bool isLittleEndian)
        {
            using (EneterTrace.Entering())
            {
                myIsLittleEndian = isLittleEndian;
            }
        }

        public object Serialize<_T>(_T dataToSerialize)
        {
            using (EneterTrace.Entering())
            {
                if (typeof(_T) != typeof(BrokerMessage))
                {
                    throw new InvalidOperationException("Only " + typeof(BrokerMessage).Name + " can be serialized.");
                }

                object aTemp = dataToSerialize;
                BrokerMessage aBrokerMessage = (BrokerMessage)aTemp;

                using (MemoryStream aStream = new MemoryStream())
                {
                    BinaryWriter aWriter = new BinaryWriter(aStream);

                    // Write broker request.
                    byte aBrokerRequestValue = (byte)aBrokerMessage.Request;
                    aWriter.Write((byte)aBrokerRequestValue);

                    // Write message types.
                    myEncoderDecoder.WriteInt32(aWriter, aBrokerMessage.MessageTypes.Length, myIsLittleEndian);

                    foreach (string aMessageType in aBrokerMessage.MessageTypes)
                    {
                        myEncoderDecoder.WritePlainString(aWriter, aMessageType, Encoding.UTF8, myIsLittleEndian);
                    }

                    // Write message.
                    if (aBrokerMessage.Request == EBrokerRequest.Publish)
                    {
                        myEncoderDecoder.Write(aWriter, aBrokerMessage.Message, myIsLittleEndian);
                    }

                    return aStream.ToArray();
                }
            }
        }

        public _T Deserialize<_T>(object serializedData)
        {
            using (EneterTrace.Entering())
            {
                if (serializedData is byte[] == false)
                {
                    throw new ArgumentException("Input parameter 'serializedData' is not byte[].");
                }

                if (typeof(_T) != typeof(BrokerMessage))
                {
                    throw new InvalidOperationException("Data can be deserialized only into" + typeof(BrokerMessage).Name);
                }

                BrokerMessage aResult;

                byte[] aData = (byte[])serializedData;

                using (MemoryStream aStream = new MemoryStream(aData))
                {
                    BinaryReader aReader = new BinaryReader(aStream);


                    // Read broker request.
                    int aBrokerRequestNumber = aReader.ReadByte();
                    EBrokerRequest aBrokerRequest = (EBrokerRequest)aBrokerRequestNumber;


                    // Read number of message types.
                    int aNumberOfMessageTypes = myEncoderDecoder.ReadInt32(aReader, myIsLittleEndian);

                    // Read message types.
                    string[] aMessageTypes = new string[aNumberOfMessageTypes];
                    for (int i = 0; i < aMessageTypes.Length; ++i)
                    {
                        string aMessageType = myEncoderDecoder.ReadPlainString(aReader, Encoding.UTF8, myIsLittleEndian);
                        aMessageTypes[i] = aMessageType;
                    }

                    if (aBrokerRequest == EBrokerRequest.Publish)
                    {
                        object aPublishedMessage = myEncoderDecoder.Read(aReader, myIsLittleEndian);
                        aResult = new BrokerMessage(aMessageTypes[0], aPublishedMessage);
                    }
                    else
                    {
                        aResult = new BrokerMessage(aBrokerRequest, aMessageTypes);
                    }

                    return (_T)(object)aResult;
                }
            }
        }


        private bool myIsLittleEndian;
        private EncoderDecoder myEncoderDecoder = new EncoderDecoder();
    }
}
