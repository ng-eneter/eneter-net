/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/


namespace Eneter.Messaging.EndPoints.Rpc
{
    internal class RpcFlags
    {
        public const int InvokeMethod = 10;
        public const int MethodResponse = 10;
        public const int SubscribeEvent = 20;
        public const int UnsubscribeEvent = 30;
        public const int RaiseEvent = 40;
    }
}
