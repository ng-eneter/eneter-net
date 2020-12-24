

using Eneter.Messaging.Diagnostic;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Eneter.Messaging.DataProcessing.Serializing
{
    internal static class SerializerExt
    {
        /// <summary>
        /// Serializes using Type instead of generics.
        /// </summary>
        /// <param name="serializer">serializer derived from ISerializer</param>
        /// <param name="dataType">type of serialized data</param>
        /// <param name="dataToSerialzier">data to be serialized</param>
        /// <returns></returns>
        public static object Serialize(this ISerializer serializer, Type dataType, object dataToSerialzier)
        {
            MethodInfo aGenericMethod;
            using (ThreadLock.Lock(mySerializeCache))
            {
                mySerializeCache.TryGetValue(dataType, out aGenericMethod);
                if (aGenericMethod == null)
                {
                    aGenericMethod = mySerializeMethod.MakeGenericMethod(dataType);
                    mySerializeCache[dataType] = aGenericMethod;
                }
            }

            object aSerializedData = aGenericMethod.Invoke(serializer, new object[] { dataToSerialzier });
            return aSerializedData;
        }

        /// <summary>
        /// Deserializes using Type instead of generics.
        /// </summary>
        /// <param name="serializer">serializer derived from ISerializer</param>
        /// <param name="dataType">type of deserialized data</param>
        /// <param name="serializedData">data to be deserialized</param>
        /// <returns></returns>
        public static object Deserialize(this ISerializer serializer, Type dataType, object serializedData)
        {
            MethodInfo aGenericMethod;
            using (ThreadLock.Lock(myDeserializeCache))
            {
                myDeserializeCache.TryGetValue(dataType, out aGenericMethod);
                if (aGenericMethod == null)
                {
                    aGenericMethod = myDeserializeMethod.MakeGenericMethod(dataType);
                    myDeserializeCache[dataType] = aGenericMethod;
                }
            }

            object aDeserializedData = aGenericMethod.Invoke(serializer, new object[] { serializedData });
            return aDeserializedData;
        }

        public static ISerializer ForResponseReceiver(this ISerializer serializer, string responseReceiverId)
        {
            ISerializer aSerializer;
            if (serializer is CallbackSerializer)
            {
                aSerializer = ((CallbackSerializer)serializer).GetSerializerCallback(responseReceiverId);
            }
            else
            {
                aSerializer = serializer;
            }

            return aSerializer;
        }

        public static bool IsSameForAllResponseReceivers(this ISerializer serializer)
        {
            return serializer is CallbackSerializer == false;
        }


        private static MethodInfo mySerializeMethod = typeof(ISerializer).GetMethod("Serialize");
        private static MethodInfo myDeserializeMethod = typeof(ISerializer).GetMethod("Deserialize");
        private static Dictionary<Type, MethodInfo> mySerializeCache = new Dictionary<Type, MethodInfo>();
        private static Dictionary<Type, MethodInfo> myDeserializeCache = new Dictionary<Type, MethodInfo>();
    }
}
