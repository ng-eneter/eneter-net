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
    /// The class is used by the eneter framework to transfer command return data.
    /// </summary>
    /// <typeparam name="_ReturnDataType">type of return data from the command</typeparam>
#if !SILVERLIGHT
    [Serializable]
#endif
    [DataContract]
    public class CommandReturnData<_ReturnDataType>
    {
        /// <summary>
        /// Default constructor for deserializer.
        /// </summary>
        public CommandReturnData()
        {
        }

        /// <summary>
        /// Constructs the class from the input parameters.
        /// </summary>
        /// <param name="commandId">command identifier</param>
        /// <param name="state">command state</param>
        /// <param name="returnData">return data</param>
        /// <param name="errorMessage">command identifier</param>
        public CommandReturnData(string commandId, ECommandState state, _ReturnDataType returnData, string errorMessage)
        {
            CommandState = state;
            ReturnData = returnData;
            ErrorMessage = errorMessage;
            CommandId = commandId;
        }

        /// <summary>
        /// State of the command.
        /// </summary>
        [DataMember]
        public ECommandState CommandState { get; set; }

        /// <summary>
        /// Return data.
        /// </summary>
        [DataMember]
        public _ReturnDataType ReturnData { get; set; }

        /// <summary>
        /// Error message from the command.
        /// </summary>
        [DataMember]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Command identifier.
        /// </summary>
        [DataMember]
        public string CommandId { get; set; }
    }
}
