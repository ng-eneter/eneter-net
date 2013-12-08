/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2011
*/

#if !SILVERLIGHT

using System.IO;

namespace Eneter.Messaging.MessagingSystems.TcpMessagingSystem.Security
{
    /// <summary>
    /// Declares the factory responsible for creating the security stream.
    /// </summary>
    /// <remarks>
    /// The security stream wrapps the source stream and provides functionality for authentication (verifying communicating parts),
    /// encryption (writes encrypted data to the wrapped stream) and decryption (decrypts data from the wrapped stream).
    /// </remarks>
    public interface ISecurityFactory
    {
        /// <summary>
        /// Creates the security stream and performs the authentication.
        /// </summary>
        /// <param name="source">The stream wrapped by the security stream.</param>
        /// <returns>Security stream.</returns>
        Stream CreateSecurityStreamAndAuthenticate(Stream source);
    }
}

#endif