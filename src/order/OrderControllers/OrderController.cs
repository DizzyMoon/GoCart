using Microsoft.AspNetCore.Mvc;
using Order.OrderModels;
using Order.OrderService;

namespace Order.OrderControllers 
{
  [ApiController]
  [Route("[controller]")]
  public class OrderController : ControllerBase
  {
    private readonly IOrderService _orderService;

    public OrderController(IOrderService orderService) 
    {
      _orderService = orderService;
    }

    /// <summary>
    /// Hent alle Orders
    /// </summary>
    /// <returns>Liste af orders returneres</returns>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OrderModel))]
    [Produces("application/json")]
    [HttpGet]
    [Route("")]
    public async Task<IActionResult> GetQueryCollection()
    {
      var result = await _orderService.GetQueryCollection();
      return Ok(result);
    }

    /// <summary>
    /// Hent en Order
    /// </summary>
    /// <param name="orderId"></param>
    /// <returns>En order returneres</returns>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OrderModel))]
    [Produces("application/json")]
    [HttpGet]
    [Route("{orderId:int}")]
    public async Task<IActionResult> Get([FromRoute] int orderId)
    {
      var result = await _orderService.Get(orderId);
      return Ok(result);
    }
    
    public async Task<IActionResult> Create([FromBody])
  }
}