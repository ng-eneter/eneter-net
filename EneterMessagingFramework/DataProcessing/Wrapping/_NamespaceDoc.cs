/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System.Runtime.CompilerServices;

namespace Eneter.Messaging.DataProcessing.Wrapping
{
    /// <summary>
    /// Functionality for wrapping and unwrapping serialized data.
    /// </summary>
    /// <remarks>
    /// The wrapping functionality allows to extended already serialized data by some additional data.
    /// <example>
    /// The example shows how to extend already serialized data by time stamp.
    /// <code>
    /// // Serialization into the xml string.
    /// XmlStringSerializer aSerializer = new XmlStringSerializer();
    /// 
    /// // Get the time stamp.
    /// DateTime aTimeStamp = DateTime.Now;
    /// 
    /// // Add the the time stamp to already serialized data.
    /// // Note: It creates the 'WrappedData' class containing both time stamp and original serialized data
    /// //       and serializes it by the provided serializer.
    /// object aWrappedSerializedData = DataWrapper.Wrap(aTimeStamp, anAlreadySerializedData, aSerializer)
    /// 
    /// ...
    /// 
    /// // Deserialization.
    /// // 1. Data must be unwrapped.
    /// WrappedData aWrappedData = DataWrapper.Unwrap(aWrappedSerializedData, aSerializer);
    /// 
    /// // 2. Read the time stamp.
    /// DateTime aTimeStamp = (DateTime)aWrappedData.AddedData;
    /// 
    /// // 3. Continue in processing of originally serialized data.
    /// object anOriginalSerializedData = aWrappedData.OriginalData;
    /// 
    /// </code>
    /// </example>
    /// </remarks>
    [CompilerGeneratedAttribute()]
    class NamespaceDoc
    {
    }
}
