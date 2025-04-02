using Microsoft.AspNetCore.Mvc;
using Order.OrderModels;
using Order.Service;

namespace Order.OrderControllers 
{
  public class OrderController : ControllerBase
  {
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService) 
    {
      _orderService = orderService;
    }

    /// <summary>
    /// Hent Orders
    /// </summary>
    /// <returns>Liste af orders returneres</returns>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<OrderModel>))]
    [Produces("application/json")]
    [HttpGet]
    [Route("")]
    public async Task<IActionResult> GetQueryCollection()
    {
      var result = await _orderService.GetQueryCollection();
      return Ok(result);
    }
  }
}