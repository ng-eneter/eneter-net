/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/

namespace Eneter.Messaging.DataProcessing.Serializing
{
    /// <summary>
    /// Callback used by communication components to get serializer for a specific client connection.
    /// </summary>
    /// <param name="responseReceiverId">specifies the particular client connection</param>
    /// <remarks>
    /// If the callback is specified then the service communication component calls it whenever it needs to serialize/deserialize
    /// the communicatation with the client. The purpose of this callback is to allow to use for each client a different serializer.
    /// E.g. if the serialized message shall be encrypted by a client specific password or a key.
    /// </remarks>
    /// <returns>serializer</returns>
    public delegate ISerializer GetSerializerCallback(string responseReceiverId);
}
