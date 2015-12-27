/*
* Project: Eneter.Messaging.Framework
* Author:  Ondrej Uzovic
* 
* Copyright © Ondrej Uzovic 2015
*/

using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;
using System;
using System.IO;
using System.Text;

namespace Eneter.Messaging.EndPoints.Rpc
{
    /// <summary>
    /// Serializer optimized for RPC.
    /// </summary>
    /// <remarks>
    /// Performance of this serializer is optimized for RPC.
    /// To increase the performance it serializes RpcMessage (which is internaly used for RPC interaction) into a special byte sequence.
    /// It uses underlying serializer to serialize method inrput paramters.
    /// </remarks>
    public class RpcCustomSerializer : ISerializer
    {
        /// <summary>
        /// Constructs the serializer.
        /// </summary>
        /// <param name="serializer">Underlying serializer used to serialize/deserialize method's input parameters.</param>
        public RpcCustomSerializer(ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                myIsLittleEndian = true;
                myUnderlyingSerializer = serializer;
            }
        }

        /// <summary>
        /// Serializes data.
        /// </summary>
        /// <typeparam name="_T">Type of serialized data.</typeparam>
        /// <param name="dataToSerialize">Data to be serialized.</param>
        /// <returns>data serialized into byte[]</returns>
        public object Serialize<_T>(_T dataToSerialize)
        {
            using (EneterTrace.Entering())
            {
                if (typeof(_T) == typeof(RpcMessage))
                {
                    RpcMessage anRpcMessage = (RpcMessage)((object)dataToSerialize);
                    return SerializeRpcMessage(anRpcMessage);
                }
                else
                {
                    return myUnderlyingSerializer.Serialize<_T>(dataToSerialize);
                }
            }
        }

        /// <summary>
        /// Deserializes data.
        /// </summary>
        /// <typeparam name="_T">Type of serialized data.</typeparam>
        /// <param name="serializedData">data to be deserialized. The expected type is byte[].</param>
        /// <returns>Deserialized object.</returns>
        public _T Deserialize<_T>(object serializedData)
        {
            using (EneterTrace.Entering())
            {
                if (typeof(_T) == typeof(RpcMessage))
                {
                    if (serializedData is byte[] == false)
                    {
                        throw new InvalidOperationException("Failed to deserialize RpcMessage because the input parameter 'serializedData' is not byte[].");
                    }

                    return (_T)(object)DeserializeRpcMessage((byte[])serializedData);
                }
                else
                {
                    return myUnderlyingSerializer.Deserialize<_T>(serializedData);
                }
            }
        }

        private object SerializeRpcMessage(RpcMessage rpcMessage)
        {
            using (MemoryStream aStream = new MemoryStream())
            {
                BinaryWriter aWriter = new BinaryWriter(aStream);

                // Write Id of the request.
                myEncoderDecoder.WriteInt32(aWriter, rpcMessage.Id, myIsLittleEndian);

                // Write request flag.
                byte aRequestType = (byte)rpcMessage.Request;
                aWriter.Write(aRequestType);

                if (rpcMessage.Request == ERpcRequest.InvokeMethod ||
                    rpcMessage.Request == ERpcRequest.RaiseEvent)
                {
                    // Write name of the method or name of the event which shall be raised.
                    myEncoderDecoder.WritePlainString(aWriter, rpcMessage.OperationName, Encoding.UTF8, myIsLittleEndian);

                    // Write number of input parameters.
                    if (rpcMessage.SerializedParams == null)
                    {
                        myEncoderDecoder.WriteInt32(aWriter, 0, myIsLittleEndian);
                    }
                    else
                    {
                        myEncoderDecoder.WriteInt32(aWriter, rpcMessage.SerializedParams.Length, myIsLittleEndian);
                        // Write already serialized input parameters.
                        for (int i = 0; i < rpcMessage.SerializedParams.Length; ++i)
                        {
                            myEncoderDecoder.Write(aWriter, rpcMessage.SerializedParams[i], myIsLittleEndian);
                        }
                    }
                }
                else if (rpcMessage.Request == ERpcRequest.SubscribeEvent ||
                         rpcMessage.Request == ERpcRequest.UnsubscribeEvent)
                {
                    // Write name of the event which shall be subscribed or unsubcribed.
                    myEncoderDecoder.WritePlainString(aWriter, rpcMessage.OperationName, Encoding.UTF8, myIsLittleEndian);
                }
                else if (rpcMessage.Request == ERpcRequest.Response)
                {
                    // Write already serialized return value.
                    if (rpcMessage.SerializedReturn != null)
                    {
                        // Indicate it is not void.
                        aWriter.Write((byte)1);

                        // Wtrite return value.
                        myEncoderDecoder.Write(aWriter, rpcMessage.SerializedReturn, myIsLittleEndian);
                    }
                    else
                    {
                        // Indicate there is no rturn value.
                        aWriter.Write((byte)0);
                    }

                    if (!string.IsNullOrEmpty(rpcMessage.ErrorType))
                    {
                        // Indicate the response contains the error message.
                        aWriter.Write((byte)1);

                        // Write error.
                        myEncoderDecoder.WritePlainString(aWriter, rpcMessage.ErrorType, Encoding.UTF8, myIsLittleEndian);
                        myEncoderDecoder.WritePlainString(aWriter, rpcMessage.ErrorMessage, Encoding.UTF8, myIsLittleEndian);
                        myEncoderDecoder.WritePlainString(aWriter, rpcMessage.ErrorDetails, Encoding.UTF8, myIsLittleEndian);
                    }
                    else
                    {
                        // Indicate the response does not contain the error message.
                        aWriter.Write((byte)0);
                    }
                }

                return aStream.ToArray();
            }
        }

        private RpcMessage DeserializeRpcMessage(byte[] data)
        {
            using (MemoryStream aStream = new MemoryStream(data))
            {
                BinaryReader aReader = new BinaryReader(aStream);

                RpcMessage anRpcMessage = new RpcMessage();

                // Read Id of the request.
                anRpcMessage.Id = myEncoderDecoder.ReadInt32(aReader, myIsLittleEndian);

                // Read request flag.
                int aRequest = aReader.ReadByte();
                anRpcMessage.Request = (ERpcRequest)aRequest;

                if (anRpcMessage.Request == ERpcRequest.InvokeMethod ||
                    anRpcMessage.Request == ERpcRequest.RaiseEvent)
                {
                    // Read name of the method or name of the event which shall be raised.
                    anRpcMessage.OperationName = myEncoderDecoder.ReadPlainString(aReader, Encoding.UTF8, myIsLittleEndian);

                    // Read number of input parameters.
                    int aNumberParameters = myEncoderDecoder.ReadInt32(aReader, myIsLittleEndian);

                    // Read input parameters.
                    anRpcMessage.SerializedParams = new object[aNumberParameters];
                    for (int i = 0; i < anRpcMessage.SerializedParams.Length; ++i)
                    {
                        anRpcMessage.SerializedParams[i] = myEncoderDecoder.Read(aReader, myIsLittleEndian);
                    }
                }
                else if (anRpcMessage.Request == ERpcRequest.SubscribeEvent ||
                         anRpcMessage.Request == ERpcRequest.UnsubscribeEvent)
                {
                    // Read name of the event which shall be subscribed or unsubcribed.
                    anRpcMessage.OperationName = myEncoderDecoder.ReadPlainString(aReader, Encoding.UTF8, myIsLittleEndian);
                }
                else if (anRpcMessage.Request == ERpcRequest.Response)
                {
                    int aReturnFlag = aReader.ReadByte();
                    if (aReturnFlag == 1)
                    {
                        anRpcMessage.SerializedReturn = myEncoderDecoder.Read(aReader, myIsLittleEndian);
                    }

                    int anErrorFlag = aReader.ReadByte();
                    if (anErrorFlag == 1)
                    {
                        anRpcMessage.ErrorType = myEncoderDecoder.ReadPlainString(aReader, Encoding.UTF8, myIsLittleEndian);
                        anRpcMessage.ErrorMessage = myEncoderDecoder.ReadPlainString(aReader, Encoding.UTF8, myIsLittleEndian);
                        anRpcMessage.ErrorDetails = myEncoderDecoder.ReadPlainString(aReader, Encoding.UTF8, myIsLittleEndian);
                    }
                }

                return anRpcMessage;
            }
        }

        private ISerializer myUnderlyingSerializer;
        private bool myIsLittleEndian;
        private EncoderDecoder myEncoderDecoder = new EncoderDecoder();
    }
}
