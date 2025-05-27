using RabbitMQ.Client;

namespace product.Messaging.Connection
{
    public interface IRabbitMqConnectionManager : IDisposable
    {
        bool IsConnected { get; }
        bool TryConnect();
        IModel CreateChannel();
        void DeclareQueuesAndBindings();
    }
}