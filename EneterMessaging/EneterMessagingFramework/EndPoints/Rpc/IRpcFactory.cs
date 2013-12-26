/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/


namespace Eneter.Messaging.EndPoints.Rpc
{
    /// <summary>
    /// Declares the factory which creates client and sercice components for the interprocess communication using RPC
    /// (Remote Procedure Call).
    /// </summary>
    public interface IRpcFactory
    {
        /// <summary>
        /// Creates the client for the given interface.
        /// </summary>
        /// <typeparam name="TServiceInterface">interface type declaring methods and events that shall be used for
        /// the interprocess communication</typeparam>
        /// <returns>RPC client component</returns>
        IRpcClient<TServiceInterface> CreateClient<TServiceInterface>() where TServiceInterface : class;

        /// <summary>
        /// Creates the service which implements the given interface.
        /// </summary>
        /// <typeparam name="TServiceInterface">interface type declaring methods and events which are exposed via the service</typeparam>
        /// <param name="service">instance implementing the service interface</param>
        /// <returns></returns>
        IRpcService<TServiceInterface> CreateService<TServiceInterface>(TServiceInterface service) where TServiceInterface : class;
    }
}