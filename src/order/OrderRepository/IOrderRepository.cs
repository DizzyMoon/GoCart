using System.Collections.Generic;
using System.Threading.Tasks;
using Order.OrderModels;

namespace Order.OrderRepository
{
  public interface IOrderRepository
  {
    Task<IEnumerable<OrderModel>> GetQueryCollection();
    Task<OrderModel?> Get(int orderId);
    Task<OrderModel> Create(OrderModel order);
    Task<OrderModel?> Delete(int orderId);
  }
}