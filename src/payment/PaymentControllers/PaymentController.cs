using Microsoft.AspNetCore.Mvc;
using payment.PaymentModels;
using payment.PaymentServices;
using Stripe;

namespace payment.PaymentControllers
{
    [ApiController]
    [Route("[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        
        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CreatePaymentResponse))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status405MethodNotAllowed)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Produces("application/json")]
        [HttpPost]
        [Route("")]
        public async Task<ActionResult<CreatePaymentResponse>> Create([FromBody] CreatePaymentRequest request)
        {
            try
            {
                var result = await _paymentService.Create(request);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status405MethodNotAllowed, new { error = ex.Message });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Unhandled exception in PaymentController: {ex.Message}");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "An unexpected server error occurred." });
            }
        }
    }
}

