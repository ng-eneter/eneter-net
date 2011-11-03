/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System;

namespace Eneter.Messaging.Diagnostic
{
    /// <summary>
    /// Internal helper class to trace typical messags.
    /// </summary>
    internal static class ErrorHandler
    {
        public const string NullOrEmptyChannelId = "Channel id is null or empty string.";
        public const string SendMessageFailure = " failed to send the message.";

        public const string IsAlreadyListening = " is already listening.";
        public const string StartListeningFailure = " failed to start listening.";
        public const string StopListeningFailure = " incorrectly stoped the listenig.";
        public const string NobodySubscribedForMessage = " received a message but nobody was subscribed to process it.";

        public const string IsAlreadyConnected = " is already connected.";
        public const string OpenConnectionFailure = " failed to open connection.";
        public const string CloseConnectionFailure = " failed to send the message that the connection was closed.";
        public const string SendMessageNotConnectedFailure = " cannot send the message when not connected.";

        public const string SendResponseFailure = " failed to send the response message.";
        public const string SendResponseNotConnectedFailure = " cannot send the response message when not connected.";

        public const string DisconnectResponseReceiverFailure = " failed to disconnect the response receiver ";
        public const string ReceiveMessageFailure = " failed to receive the message.";
        public const string ReceiveMessageIncorrectFormatFailure = " failed to receive the message because the message came in incorrect format.";

        public const string InvalidUriAddress = " is not valid URI address.";

        public const string StopThreadFailure = " failed to stop the thread with id ";
        public const string AbortThreadFailure = " failed to abort the thread.";
        public const string UnregisterMessageHandlerThreadFailure = " failed to unregister the handler of messages.";
        public const string DoListeningFailure = " failed in the loop listening to messages.";

        public const string ProcessingHttpConnectionFailure = " detected a failure during processing Http connection.";
        public const string ProcessingTcpConnectionFailure = " detected a failure during processing Tcp connection.";


        public const string DetectedException = " detected an exception.";
    }
}
