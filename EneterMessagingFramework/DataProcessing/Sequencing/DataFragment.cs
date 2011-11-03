/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using System.Runtime.Serialization;

namespace Eneter.Messaging.DataProcessing.Sequencing
{
    /// <summary>
    /// Implements the data fragment for specified data type.
    /// </summary>
    /// <typeparam name="_DataType">data type contained in the fragment</typeparam>
#if !SILVERLIGHT
    [Serializable]
#endif
    [DataContract]
    public class DataFragment<_DataType> : Fragment
    {
        /// <summary>
        /// Default constructor used for deserialization.
        /// </summary>
        public DataFragment()
        {
        }

        /// <summary>
        /// Constructs the fragment from parameters.
        /// </summary>
        /// <param name="data">Data contained in the fragment.</param>
        /// <param name="sequenceId">Identifies the sequence where the fragment belongs.</param>
        /// <param name="index">Number of the fragment.</param>
        /// <param name="isFinal">Indicates whether the fragmant is the last one.</param>
        public DataFragment(_DataType data, string sequenceId, int index, bool isFinal)
            : base(sequenceId, index, isFinal)
        {
            Data = data;
        }

        /// <summary>
        /// Data contained in the fragment.
        /// </summary>
        [DataMember]
        public _DataType Data { get; set; }
    }
}
