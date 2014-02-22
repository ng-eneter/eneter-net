/*
 * Project: Eneter.Messaging.Framework
 * Author:  Ondrej Uzovic
 * 
 * Copyright © Ondrej Uzovic 2013
*/


namespace Eneter.Messaging.EndPoints.Rpc
{
    /// <summary>
    /// Creates services and clients that can communicate using Remote Procedure Calls.
    /// </summary>
    public interface IRpcFactory
    {
        /// <summary>
        /// Creates RPC client for the given interface.
        /// </summary>
        /// <typeparam name="TServiceInterface">service interface type.</typeparam>
        /// <returns>RpcClient instance</returns>
        IRpcClient<TServiceInterface> CreateClient<TServiceInterface>() where TServiceInterface : class;

        /// <summary>
        /// Creates RPC service for the given interface.
        /// </summary>
        /// <typeparam name="TServiceInterface">service interface type</typeparam>
        /// <param name="service">instance implementing the given service interface</param>
        /// <returns>RpcService instance.</returns>
        IRpcService<TServiceInterface> CreateService<TServiceInterface>(TServiceInterface service) where TServiceInterface : class;
    }
}