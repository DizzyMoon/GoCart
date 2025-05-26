using RabbitMQ.Client;

namespace payment.Messaging.Connection
{
    public interface IRabbitMqConnectionManager : IDisposable
    {
        bool IsConnected { get; }
        bool TryConnect();
        IModel CreateChannel();
        void DeclarePrimaryExchanges();
    }
}