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

    public async Task<OrderModel> Create(CreateOrderModel dto)
    {
      string orderNumber = Guid.NewGuid().ToString().ToUpper();
      DateTime orderDate = DateTime.Now;

      var newOrder = new OrderModel
      {
        OrderNumber = orderNumber,
        OrderDate = orderDate
      };

      return await _orderRepository.Create(newOrder);
    }
  }
}