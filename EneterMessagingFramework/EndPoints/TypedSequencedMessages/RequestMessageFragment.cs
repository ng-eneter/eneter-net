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
    /// Data used for the typed sequenced message fragment.
    /// The data is used to send the message fragment from a duplex output channel to a duplex input receiver.
    /// </summary>
    /// <typeparam name="_RequestType">message type</typeparam>
#if !SILVERLIGHT
    [Serializable]
#endif
    [DataContract]
    public class RequestMessageFragment<_RequestType> : Fragment
    {
        /// <summary>
        /// Default constructor for the deserialization.
        /// </summary>
        public RequestMessageFragment()
        {
        }

        /// <summary>
        /// Constructor creating the typed message fragment from the given input parameters.
        /// </summary>
        public RequestMessageFragment(_RequestType fragmentData, string sequenceId, int index, bool isFinal)
            : base(sequenceId, index, isFinal)
        {
            FragmentData = fragmentData;
        }

        /// <summary>
        /// Returns serialized fragment value.
        /// </summary>
        [DataMember]
        public _RequestType FragmentData { get; set; }
    }
}
