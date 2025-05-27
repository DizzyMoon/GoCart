using payment.Messaging.Events;
using System.Threading.Tasks;

namespace payment.Messaging.Publishers
{
    public interface IMessagePublisher
    {
        Task PublishPaymentSucceededEventAsync(PaymentSucceededEvent eventData);
        Task PublishPaymentFailedEventAsync(PaymentFailedEvent eventData);
    }
}