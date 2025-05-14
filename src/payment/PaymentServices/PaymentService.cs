using payment.PaymentModels;
using Stripe;

namespace payment.PaymentServices;

public class PaymentService : IPaymentService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentService> _logger;
    private readonly PaymentIntentService _paymentIntentService;

    public PaymentService(IConfiguration configuration, ILogger<PaymentService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _paymentIntentService = new PaymentIntentService();
    }

    public async Task<CreatePaymentResponse> Create(CreatePaymentRequest request)
    {
        _logger.LogInformation("Performing validation in PaymentService.");

        if (request == null)
        {
            _logger.LogError("Validation Failed: Payment creation request is null.");
            throw new InvalidOperationException("Payment request data is missing.");
        }

        if (request.Amount <= 0)
        {
            _logger.LogError($"Validation Failed: Invalid amount received: {request.Amount}");
            throw new InvalidCastException("Amount must be positive.");
        }

        if (string.IsNullOrEmpty(request.Currency))
        {
            _logger.LogError("Validation Failed: Currency is missing from payment request.");
            throw new InvalidOperationException("Currency is required.");
        }
        
        _logger.LogInformation("Validation successful. Preparing Stripe API call.");

        var options = new PaymentIntentCreateOptions
        {
            Amount = request.Amount,
            Currency = request.Currency,
            PaymentMethodTypes = ["card"],
        };

        try
        {
            _logger.LogInformation(
                $"Calling Stripe API to create PaymentIntent for {request.Amount} {request.Currency}...");
            var paymentIntent = await _paymentIntentService.CreateAsync(options);
            _logger.LogInformation(
                $"Successfully created PaymentIntent with ID: {paymentIntent.Id}. Status: {paymentIntent.Status}");

            return new CreatePaymentResponse
            {
                ClientSecret = paymentIntent.ClientSecret,
                PaymentIntentId = paymentIntent.Id,
                Amount = paymentIntent.Amount,
                Currency = paymentIntent.Currency
            };
        }
        catch (StripeException e)
        {
            _logger.LogError(e, $"Stripe API error during PaymentIntent creation: {e.Message}");
            throw new InvalidOperationException($"Stripe API error: {e.Message}", e);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unexpected error during PaymentIntent creation process.");
            throw new InvalidOperationException($"An unexpected server error occurred: {e.Message}", e);
        }
    }
}