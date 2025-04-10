using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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

    /// <summary>
    /// Opret en ny Order
    /// </summary>
    /// <response code="200">Success</response>
    /// <response code="405">Method Not Allowed</response>
    /// <returns>Nye order returneres</returns>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OrderModel))]
    [ProducesResponseType(StatusCodes.Status405MethodNotAllowed, Type = typeof(OrderModel))]
    [Produces("application/json")]
    [HttpPost]
    [Route("")]
    public async Task<IActionResult> Create()
    {
      try
      {
        var result = await _orderService.Create(new CreateOrderModel());
        return Ok(result);
      }
      catch (InvalidOperationException ex)
      {
        return StatusCode(StatusCodes.Status405MethodNotAllowed, ex.Message);
      }
    }

    /// <summary>
    /// Slet en Order
    /// </summary>
    /// <param name="orderId"></param>
    /// <response code="200">Success</response>
    /// <returns>Slettet order returneres</returns>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OrderModel))]
    [Produces("application/json")]
    [HttpDelete]
    [Route("{orderId:int}")]
    public async Task<IActionResult> Delete([FromRoute] int orderId)
    {
      var result = await _orderService.Delete(orderId);
      return Ok(result);
    }
  }
}