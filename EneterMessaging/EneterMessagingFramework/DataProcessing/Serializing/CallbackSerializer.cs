/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/

using Eneter.Messaging.Diagnostic;
using System;

namespace Eneter.Messaging.DataProcessing.Serializing
{
    /// <summary>
    /// Place holder serializer which contains the callback for the serialization.
    /// </summary>
    /// <remarks>
    /// The serializer is used in cases where the serializer needs to be retrieved on demand based on response receiver id.
    /// </remarks>
    internal class CallbackSerializer : ISerializer
    {
        public CallbackSerializer(GetSerializerCallback getSerializerCallback)
        {
            using (EneterTrace.Entering())
            {
                if (getSerializerCallback == null)
                {
                    throw new ArgumentNullException("getSerializerCallback is null");
                }
                GetSerializerCallback = getSerializerCallback;
            }
        }

        public object Serialize<_T>(_T dataToSerialize)
        {
            using (EneterTrace.Entering())
            {
                throw new NotSupportedException();
            }
        }

        public T Deserialize<T>(object serializedData)
        {
            using (EneterTrace.Entering())
            {
                throw new NotSupportedException();
            }
        }


        public GetSerializerCallback GetSerializerCallback { get; private set; }
    }
}
