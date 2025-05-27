using product.Messaging.Events;

namespace product.Messaging.Publishers
{
    public interface IMessagePublisher
    {
        Task AddProductSucceededEventAsync(AddProductSucceededEvent eventData);
        Task AddProductFailedEventAsync(AddProductFailedEvent eventData);
    }
}