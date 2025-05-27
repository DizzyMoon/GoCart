using System.Collections.Generic;
using System.Threading.Tasks;
using order.Messaging.Events;
using Order.OrderModels;

namespace Order.OrderService
{
  public interface IOrderService
  {
    Task<IEnumerable<OrderModel>> GetQueryCollection();
    Task<OrderModel?> Get(int orderId);
    Task<OrderModel> Delete(int orderId);

    Task<OrderModel?> ProcessSuccessfulPaymentEventAsync(PaymentSucceededEvent paymentSucceededEvent);
    Task ProcessFailedPaymentEventAsync(PaymentFailedEvent paymentFailedEvent);
  }
  
}