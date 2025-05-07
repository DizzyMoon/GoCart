using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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

    public async Task<OrderModel?> Get(int orderId)
    {
      var order = await _orderRepository.Get(orderId);

      if (order == null)
      {
        throw new KeyNotFoundException($"Order with ID {orderId} not found.");
      }

      return order;
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

    public async Task<OrderModel> Delete(int orderId)
    {
      var orderToDelete = await _orderRepository.Delete(orderId);

      if (orderToDelete == null)
      {
        throw new KeyNotFoundException($"Order with ID {orderId} not found.");
      }

      return orderToDelete;
    }
  }
}