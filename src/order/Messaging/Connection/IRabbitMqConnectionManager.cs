using RabbitMQ.Client;

namespace order.Messaging.Connection
{
    public interface IRabbitMqConnectionManager : IDisposable
    {
        bool IsConnected { get; }
        bool TryConnect();
        IModel CreateChannel();
        void DeclareQueuesAndBindings();
    }
}