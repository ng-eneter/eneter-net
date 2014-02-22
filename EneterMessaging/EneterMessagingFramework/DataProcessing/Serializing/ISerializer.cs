/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/


namespace Eneter.Messaging.DataProcessing.Serializing
{
    /// <summary>
    /// Declares the serializer.
    /// </summary>
    public interface ISerializer
    {
        /// <summary>
        /// Serializes data.
        /// </summary>
        /// <typeparam name="_T">Type of serialized data.</typeparam>
        /// <param name="dataToSerialize">Data to be serialized.</param>
        /// <returns>
        /// Object representing the serialized data.
        /// Typically it can be byte[] or string.
        /// </returns>
        object Serialize<_T>(_T dataToSerialize);

        /// <summary>
        /// Deserializes data.
        /// </summary>
        /// <typeparam name="_T">Type of serialized data.</typeparam>
        /// <param name="serializedData">Data to be deserialized.</param>
        /// <returns>Deserialized object.</returns>
        _T Deserialize<_T>(object serializedData);
    }
}
