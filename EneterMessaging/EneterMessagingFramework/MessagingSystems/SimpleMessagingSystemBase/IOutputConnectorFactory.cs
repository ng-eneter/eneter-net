


namespace Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase
{
    internal interface IOutputConnectorFactory
    {
        IOutputConnector CreateOutputConnector(string inputConnectorAddress, string outputConnectorAddress);
    }
}
