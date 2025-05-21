using RabbitMQ.Client;

namespace payment.Messaging
{
    public interface IRabbitMqPublisher
    {
        void PublishPaymentSuccess(string paymentIntentId, long amount, string currency);
        void PublishPaymentFailedToOrder(string paymentIntentId, string reason);
    }
}