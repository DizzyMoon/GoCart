using Order.OrderModels;

namespace Order.orderService
{
  public interface IOrderService
  {
    Task<IEnumerable<OrderModel>> GetQueryCollection();
  }
  
}