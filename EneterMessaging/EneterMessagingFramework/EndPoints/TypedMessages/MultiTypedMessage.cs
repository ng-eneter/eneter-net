/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2015
*/

namespace Eneter.Messaging.EndPoints.TypedMessages
{
    public class MultiTypedMessage
    {
        public string TypeName { get; set; }
        public object MessageData { get; set; }
    }
}
