using System;

namespace Order.OrderModels
{
  public class OrderModel
  {
    public int Id { get; set; }
    public string OrderNumber { get; set; }
    public DateTime OrderDate { get; set; }
  }

  public class CreateOrderModel
  {
    public string OrderNumber { get; set; }
    public DateTime OrderDate { get; set; }
  }
}