using Order.OrderModels;
using order.OrderRepository;
using Order.orderService;

namespace order.OrderService
{
  public class OrderService : IOrderService
  {
    private readonly IOrderRepository _orderRepository;

    public OrderService(IOrderRepository orderRepository)
    {
      _orderRepository = orderRepository;
    }

    public async Task<IEnumerable<OrderModel>> GetQueryCollection()
    {
      return await _orderRepository.GetQueryCollection();
    }
  }
}