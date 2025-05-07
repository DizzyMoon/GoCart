using System.Collections.Generic;
using System.Threading.Tasks;
using Order.OrderModels;

namespace Order.OrderService
{
  public interface IOrderService
  {
    Task<IEnumerable<OrderModel>> GetQueryCollection();
    Task<OrderModel?> Get(int orderId);
    Task<OrderModel> Create(CreateOrderModel dto);
    Task<OrderModel> Delete(int orderId);
  }
  
}