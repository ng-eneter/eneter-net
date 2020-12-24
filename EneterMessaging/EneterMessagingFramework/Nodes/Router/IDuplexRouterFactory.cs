


namespace Eneter.Messaging.Nodes.Router
{
    /// <summary>
    /// Declares the factory creating duplex router.
    /// </summary>
    public interface IDuplexRouterFactory
    {
        /// <summary>
        /// Creates the duplex router.
        /// </summary>
        /// <returns>duplex router</returns>
        IDuplexRouter CreateDuplexRouter();
    }
}
