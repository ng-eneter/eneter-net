/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using System.Runtime.Serialization;

namespace Eneter.Messaging.Nodes.ChannelWrapper
{
    /// <summary>
    /// The data structure representing the wrapped data.
    /// </summary>
    [Serializable]
    [DataContract]
    public class WrappedData
    {
        /// <summary>
        /// Default constructor used for the deserialization.
        /// </summary>
        public WrappedData()
        {
        }

        /// <summary>
        /// Constructs wrapped data from input parameters.
        /// </summary>
        /// <param name="addedData">new data added to the original data</param>
        /// <param name="originalData">original data</param>
        public WrappedData(object addedData, object originalData)
        {
            AddedData = addedData;
            OriginalData = originalData;
        }

        /// <summary>
        /// Newly added data.
        /// </summary>
        [DataMember]
        public object AddedData { get; set; }

        /// <summary>
        /// Original (wrapped) data.
        /// </summary>
        [DataMember]
        public object OriginalData { get; set; }
    }
}
