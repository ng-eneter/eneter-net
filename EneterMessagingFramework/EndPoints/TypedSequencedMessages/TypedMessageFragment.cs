/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using System.Runtime.Serialization;
using Eneter.Messaging.DataProcessing.Sequencing;

namespace Eneter.Messaging.EndPoints.TypedSequencedMessages
{
    /// <summary>
    /// The message fragment for the specified message type.
    /// The message fragment wrapps the specified message type so that it can be sent as a part of a sequence.
    /// </summary>
#if !SILVERLIGHT
    [Serializable]
#endif
    [DataContract]
    public class TypedMessageFragment<_MessageDataType> : Fragment
    {
        /// <summary>
        /// Default constructor for the deserialization.
        /// </summary>
        public TypedMessageFragment()
        {
        }

        /// <summary>
        /// Constructor creating the typed message fragment from the given input parameters.
        /// </summary>
        public TypedMessageFragment(_MessageDataType fragmentData, string sequenceId, int index, bool isFinal)
            : base(sequenceId, index, isFinal)
        {
            FragmentData = fragmentData;
        }

        /// <summary>
        /// Returns serialized fragment value.
        /// </summary>
        [DataMember]
        public _MessageDataType FragmentData { get; set; }
    }
}
