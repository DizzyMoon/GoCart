using Order.OrderModels;

namespace order.OrderRepository
{
  public interface IOrderRepository
  {
    public Task<IEnumerable<OrderModel>> GetQueryCollection();
  }
}