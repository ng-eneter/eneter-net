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
    /// Extension providing buffering of sent messages for cases the connection is not available.
    /// </summary>
    /// <remarks>
    /// The buffered messaging is intended to temporarily store sent messages until the network connection is established.<br/>
    /// Typical scenarios are:
    /// <br/><br/>
    /// <b>Short disconnections</b><br/>
    /// In case of unstable the network the connection can broken. Buffered messaging will try to reconnect the broken connection
    /// and meanwhile it will store sent messages in the buffer. Then when the connection is repaired it will send messages from
    /// the buffer.
    /// <br/><br/>
    /// <b>Independent startup order</b><br/>
    /// It can be tricky to start communicating application in a defined order. Buffered messaging allows to start
    /// applications in undefined order. If messages are sent to an application which is not started yet they will be stored
    /// in the buffer until the application is started.
    /// 
    /// 
    /// </remarks>
    [CompilerGeneratedAttribute()]
    class NamespaceDoc
    {
    }
}
