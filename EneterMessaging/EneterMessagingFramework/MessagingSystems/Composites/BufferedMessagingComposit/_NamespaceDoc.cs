/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

using System.Runtime.CompilerServices;

namespace Eneter.Messaging.MessagingSystems.Composites.BufferedMessagingComposit
{
    /// <summary>
    /// Extension for automatic reconnect and buffering of sent messages in case of a disconnection.
    /// </summary>
    /// <remarks>
    /// The buffered messaging is intended to temporarily store sent messages until the network connection is established.<br/>
    /// Typical scenarios are:
    /// <br/><br/>
    /// <b>Short disconnections</b><br/>
    /// In case of unstable network the connection can be broken. Buffered messaging will try to recover the broken connection
    /// and meanwhile it will store sent messages to the buffer. Then when the connection is reopen it will send messages from
    /// the buffer.
    /// <br/><br/>
    /// <b>Independent startup order</b><br/>
    /// In case your SW system consists of multiple applications which need to communicate it can be problematic
    /// to start them in a certain order so that communicating parts are available.
    /// To get rid of the startup order dependency you can use the buffered messaging. If messages are sent
    /// to an application which is not started yet they will be stored in the buffer until the application is started.
    /// 
    /// 
    /// </remarks>
    [CompilerGeneratedAttribute()]
    class NamespaceDoc
    {
    }
}
