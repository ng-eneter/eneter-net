

using Eneter.Messaging.DataProcessing.Serializing;
using Eneter.Messaging.Diagnostic;

namespace Eneter.Messaging.Nodes.ChannelWrapper
{
    /// <summary>
    /// Implements the wrapper/unwrapper of data.
    /// </summary>
    internal static class DataWrapper
    {
        /// <summary>
        /// Adds the data to already serialized data.
        /// </summary>
        /// <remarks>
        /// It creates the <see cref="WrappedData"/> from the given data and serializes it with the provided serializer.<br/>
        /// </remarks>
        /// <param name="addedData">Added data. It must a basic .Net type. Otherwise the serialization will fail.</param>
        /// <param name="originalData">Already serialized data - it is type of string or byte[].</param>
        /// <param name="serializer">serializer</param>
        public static object Wrap(object addedData, object originalData, ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                WrappedData aWrappedData = new WrappedData(addedData, originalData);
                return serializer.Serialize<WrappedData>(aWrappedData);
            }
        }

        /// <summary>
        /// Takes the serialized WrappedData and deserializes it with the given serializer.
        /// </summary>
        /// <param name="wrappedData">data serialized by 'Wrap' method</param>
        /// <param name="serializer">serializer</param>
        /// <returns>deserialized WrappedData</returns>
        public static WrappedData Unwrap(object wrappedData, ISerializer serializer)
        {
            using (EneterTrace.Entering())
            {
                return serializer.Deserialize<WrappedData>(wrappedData);
            }
        }
    }
}
