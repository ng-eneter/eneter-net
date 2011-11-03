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
    /// Data used for the typed sequenced response message fragment.
    /// The data is used to send response message from a duplex input channel to a duplex output channel.
    /// </summary>
    /// <typeparam name="_ResponseType"></typeparam>
#if !SILVERLIGHT
    [Serializable]
#endif
    [DataContract]
    public class ResponseMessageFragment<_ResponseType> : Fragment
    {
        /// <summary>
        /// Default constructor for the deserialization.
        /// </summary>
        public ResponseMessageFragment()
        {
        }

        /// <summary>
        /// Constructor creating the typed message fragment from the given input parameters.
        /// </summary>
        public ResponseMessageFragment(_ResponseType fragmentData, string sequenceId, int index, bool isFinal)
            : base(sequenceId, index, isFinal)
        {
            FragmentData = fragmentData;
        }

        /// <summary>
        /// Returns serialized fragment value.
        /// </summary>
        [DataMember]
        public _ResponseType FragmentData { get; set; }
    }
}
