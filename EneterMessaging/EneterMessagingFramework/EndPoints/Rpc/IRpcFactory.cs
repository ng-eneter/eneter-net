


using System;

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
        /// Creates single-instance RPC service for the given interface.
        /// </summary>
        /// <remarks>
        /// Single-instance means that there is one instance of the service shared by all clients.
        /// </remarks>
        /// <typeparam name="TServiceInterface">service interface type</typeparam>
        /// <param name="service">instance implementing the given service interface</param>
        /// <returns>RpcService instance.</returns>
        IRpcService<TServiceInterface> CreateSingleInstanceService<TServiceInterface>(TServiceInterface service) where TServiceInterface : class;

        /// <summary>
        /// Creates per-client-instance RPC service for the given interface.
        /// </summary>
        /// <remarks>
        /// Per-client-instance means that for each connected client is created a separate instace of the service.
        /// </remarks>
        /// <typeparam name="TServiceInterface">service interface type</typeparam>
        /// <param name="serviceFactoryMethod">factory method used to create the service instance when the client is connected</param>
        /// 
        /// <returns></returns>
        IRpcService<TServiceInterface> CreatePerClientInstanceService<TServiceInterface>(Func<TServiceInterface> serviceFactoryMethod) where TServiceInterface : class;
    }
}