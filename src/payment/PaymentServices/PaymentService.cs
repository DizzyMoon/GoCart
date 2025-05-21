using System.Text.RegularExpressions;
using payment.PaymentModels;
using Stripe;

namespace payment.PaymentServices;

public class PaymentService : IPaymentService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentService> _logger;
    private readonly PaymentIntentService _paymentIntentService;
    private readonly PaymentMethodService _paymentMethodService;

    public PaymentService(
        IConfiguration configuration, 
        ILogger<PaymentService> logger, 
        PaymentIntentService paymentIntentService,
        PaymentMethodService paymentMethodService)
    {
        _configuration = configuration;
        _logger = logger;
        _paymentIntentService = paymentIntentService;
        _paymentMethodService = paymentMethodService;
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
        
        // --- Validation for Token ---
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            _logger.LogError("Validation Failed: Payment token is missing.");
            throw new InvalidOperationException("A payment token is required.");
        }
        
        _logger.LogInformation("Validation successful. Preparing Stripe API call.");

        string paymentMethodId;

        try
        {
            _logger.LogInformation("Creating Stripe PaymentMethod from provided card details.");
            var paymentMethodOptions = new PaymentMethodCreateOptions
            {
                Type = "card",
                Card = new PaymentMethodCardOptions
                {
                    Token = request.Token
                },
                BillingDetails = new PaymentMethodBillingDetailsOptions
                {
                    Name = request.CardholderName
                }
            };

            var paymentMethod = await _paymentMethodService.CreateAsync(paymentMethodOptions);
            paymentMethodId = paymentMethod.Id;
            _logger.LogInformation($"Successfully created PaymentMethod with ID: {paymentMethodId}");
        }
        catch (StripeException e)
        {
            _logger.LogError(e, $"Stripe API error during PaymentMethod creation: {e.Message}");
            throw new InvalidOperationException($"Stripe API error creating PaymentMethod: {e.Message}", e);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unexpected error during PaymentMethod creation process.");
            throw new InvalidOperationException($"An unexpected server error occurred during PaymentMethod creation: {e.Message}", e);
        }

        var paymentIntentOptions = new PaymentIntentCreateOptions
        {
            Amount = request.Amount,
            Currency = request.Currency,
            PaymentMethod = paymentMethodId,
            Confirm = true,
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
                AllowRedirects = "never"
            }
        };

        try
        {
            _logger.LogInformation(
                $"Calling Stripe API to create and confirm PaymentIntent for {request.Amount} {request.Currency}...");
            var paymentIntent = await _paymentIntentService.CreateAsync(paymentIntentOptions);
            _logger.LogInformation(
                $"Successfully created PaymentIntent with ID: {paymentIntent.Id}. Status: {paymentIntent.Status}");

            if (paymentIntent.Status == "succeeded" || paymentIntent.Status == "requires_capture")
            {
                return new CreatePaymentResponse
                {
                    PaymentIntentId = paymentIntent.Id,
                    Amount = paymentIntent.Amount,
                    Currency = paymentIntent.Currency
                };
            }
            _logger.LogWarning($"PaymentIntent ID: {paymentIntent.Id} ended with unexpected status: {paymentIntent.Status}");
            throw new InvalidOperationException($"Payment failed with status: {paymentIntent.Status}. Message: {paymentIntent.LastPaymentError?.Message}");
            
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