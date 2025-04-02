using Order.OrderModels;

namespace Order.OrderService
{
  public interface IOrderService
  {
    Task<IEnumerable<OrderModel>> GetQueryCollection();
  }
  
}