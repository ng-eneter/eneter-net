/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2012
*/

#if !WINDOWS_PHONE_70

namespace Eneter.Messaging.MessagingSystems.WebSocketMessagingSystem
{
    internal class WebSocketFrame
    {
        public WebSocketFrame(EFrameType frameType, bool maskFlag, byte[] message, bool isFinal)
        {
            FrameType = frameType;
            MaskFlag = maskFlag;
            Message = message;
            IsFinal = isFinal;
        }

        public EFrameType FrameType { get; private set; }
        public bool MaskFlag { get; private set; }
        public byte[] Message { get; private set; }
        public bool IsFinal { get; private set; }
    }
}

#endif