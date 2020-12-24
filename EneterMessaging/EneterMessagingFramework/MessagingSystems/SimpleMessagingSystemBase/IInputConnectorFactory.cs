



namespace Eneter.Messaging.MessagingSystems.SimpleMessagingSystemBase
{
    internal interface IInputConnectorFactory
    {
        IInputConnector CreateInputConnector(string inputConnectorAddress);
    }
}
