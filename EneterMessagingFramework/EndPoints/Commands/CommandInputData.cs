/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2010
*/

using System;
using System.Runtime.Serialization;

namespace Eneter.Messaging.EndPoints.Commands
{
    /// <summary>
    /// The class is used by the eneter framework to transfer a request to the command.
    /// </summary>
    /// <typeparam name="_InputDataType">type of input data</typeparam>
#if !SILVERLIGHT
    [Serializable]
#endif
    [DataContract]
    public class CommandInputData<_InputDataType>
    {
        /// <summary>
        /// Default constructor for deserialization.
        /// </summary>
        public CommandInputData()
        {
        }

        /// <summary>
        /// Constructs from the input parameters.
        /// </summary>
        /// <param name="commandId">command identifier</param>
        /// <param name="request">type of the request</param>
        /// <param name="inputData">input data</param>
        public CommandInputData(string commandId, ECommandRequest request, _InputDataType inputData)
        {
            Request = request;
            InputData = inputData;
            CommandId = commandId;
        }

        /// <summary>
        /// Type of the request.
        /// </summary>
        [DataMember]
        public ECommandRequest Request { get; set; }

        /// <summary>
        /// Input data.
        /// </summary>
        [DataMember]
        public _InputDataType InputData { get; set; }

        /// <summary>
        /// Command identifier.
        /// </summary>
        [DataMember]
        public string CommandId { get; set; }
    }
}
