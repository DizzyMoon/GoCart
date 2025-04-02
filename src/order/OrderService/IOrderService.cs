using Order.OrderModels;

namespace Order.Service
{
  public interface IOrderService
  {
    Task<IEnumerable<OrderModel>> GetQueryCollection();
  }
  
}