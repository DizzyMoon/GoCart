using Order.OrderModels;

namespace Order.OrderRepository
{
  public interface IOrderRepository
  {
    public Task<IEnumerable<OrderModel>> GetQueryCollection();
    public Task<OrderModel> Get(int orderId);
  }
}