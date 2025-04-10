using System.Collections;
using System.Threading.Tasks;
using Order.OrderModels;

namespace Order.OrderService
{
  public interface IOrderService
  {
    Task<IEnumerable> GetQueryCollection();
    Task<OrderModel> Get(int orderId);
    Task<OrderModel> Create(CreateOrderModel dto);
    Task<OrderModel> Delete(int orderId);
  }
  
}