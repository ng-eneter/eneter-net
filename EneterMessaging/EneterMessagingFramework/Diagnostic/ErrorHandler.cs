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

        public const string FailedToSendMessage = "failed to send the message.";

        public const string IsAlreadyListening = "is already listening.";
        public const string FailedToStartListening = "failed to start listening.";
        public const string IncorrectlyStoppedListening = "incorrectly stoped the listenig.";
        public const string NobodySubscribedForMessage = "received a message but nobody was subscribed to process it.";

        public const string IsAlreadyConnected = "is already connected.";
        public const string FailedToOpenConnection = "failed to open connection.";
        public const string FailedToCloseConnection = "failed to close connection.";
        public const string FailedToSendMessageBecauseNotConnected = "cannot send the message when not connected.";

        public const string FailedToSendMessageBecauseNotAttached = "failed to send the request message because the output channel is not attached.";

        public const string FailedToSendResponseMessage = "failed to send the response message.";
        public const string FailedToSendResponseBecauaeClientNotConnected = "cannot send the response message when client is not connected.";
        public const string FailedToSendResponseBecauseNotListening = "cannot send the response message when duplex input channel is not listening.";

        public const string FailedToDisconnectResponseReceiver = "failed to disconnect the response receiver ";
        public const string FailedToReceiveMessage = "failed to receive the message.";
        public const string FailedToReceiveMessageBecauseIncorrectFormat = "failed to receive the message because the message came in incorrect format.";

        public const string InvalidUriAddress = "is not valid URI address.";

        public const string FailedToStopThreadId = "failed to stop the thread with id ";
        public const string FailedToAbortThread = "failed to abort the thread.";
        public const string FailedToUnregisterMessageHandler = "failed to unregister the handler of messages.";
        public const string FailedInListeningLoop = "failed in the loop listening to messages.";

        public const string ProcessingHttpConnectionFailure = "detected a failure during processing Http connection.";
        public const string ProcessingTcpConnectionFailure = "detected a failure during processing Tcp connection.";


        public const string DetectedException = "detected an exception.";
    }
}
