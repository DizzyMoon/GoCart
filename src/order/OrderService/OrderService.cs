using Order.OrderModels;
using Order.OrderRepository;
using Order.OrderService;

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

    public async Task<OrderModel> Get(int orderId)
    {
      return await _orderRepository.Get(orderId);
    }
  }
}