using RabbitMQ.Client;

namespace payment.Messaging
{
    public interface IRabbitMqConnectionManager : IDisposable
    {
        IModel CreateChannel();
        void DeclareExchangesAndQueues();
    }
}