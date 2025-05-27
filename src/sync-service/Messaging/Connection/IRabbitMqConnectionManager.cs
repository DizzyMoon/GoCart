using RabbitMQ.Client;

namespace sync_service.Messaging.Connection
{
    public interface IRabbitMqConnectionManager : IDisposable
    {
        bool IsConnected { get; }
        bool TryConnect();
        IModel CreateChannel();
        void DeclareQueuesAndBindings();
    }
}