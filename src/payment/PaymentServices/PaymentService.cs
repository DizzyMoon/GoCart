using payment.Messaging.Events;
using payment.Messaging.Publishers;
using payment.PaymentModels;
using Stripe;

namespace payment.PaymentServices
{
    public class PaymentService : IPaymentService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PaymentService> _logger;
        private readonly PaymentIntentService _paymentIntentService;
        private readonly PaymentMethodService _paymentMethodService;
        private readonly IMessagePublisher _messagePublisher;

        public PaymentService(
            IConfiguration configuration,
            ILogger<PaymentService> logger,
            PaymentIntentService paymentIntentService,
            PaymentMethodService paymentMethodService,
            IMessagePublisher messagePublisher)
        {
            _configuration = configuration;
            _logger = logger;
            _paymentIntentService = paymentIntentService;
            _paymentMethodService = paymentMethodService;
            _messagePublisher = messagePublisher;
        }

        private void ValidatePaymentRequest(CreatePaymentRequest request)
        {
            _logger.LogInformation("Performing validation for payment request.");

            if (request == null)
            {
                _logger.LogError("Validation Failed: Payment creation request is null.");
                throw new ArgumentNullException(nameof(request), "Payment request data is missing.");
            }

            if (request.Amount <= 0)
            {
                _logger.LogError("Validation Failed: Invalid amount received: {Amount}", request.Amount);
                throw new ArgumentOutOfRangeException(nameof(request.Amount), "Amount must be positive.");
            }

            if (string.IsNullOrWhiteSpace(request.Currency))
            {
                _logger.LogError("Validation Failed: Currency is missing from payment request.");
                throw new ArgumentException("Currency is required.", nameof(request.Currency));
            }

            if (string.IsNullOrWhiteSpace(request.Token))
            {
                _logger.LogError("Validation Failed: Payment token is missing.");
                throw new ArgumentException("A payment token is required.", nameof(request.Token));
            }
            
            if (string.IsNullOrWhiteSpace(request.CardholderName))
            {
                _logger.LogError("Validation Failed: Cardholder name is missing.");
                throw new ArgumentException("A cardholder name is required.", nameof(request.CardholderName));
            }
            _logger.LogInformation("Validation successful for payment request.");
        }

        private async Task HandlePaymentErrorAsync(string paymentAttemptRef, string errorMessage, Exception? exceptionDetails = null)
        {
            if (exceptionDetails != null)
            {
                _logger.LogError(exceptionDetails, "Payment Error for PaymentAttemptRef {PaymentAttemptRef}: {ErrorMessage}", paymentAttemptRef, errorMessage);
            }
            else
            {
                _logger.LogError("Payment Error for PaymentAttemptRef {PaymentAttemptRef}: {ErrorMessage}", paymentAttemptRef, errorMessage);
            }

            var failedEvent = new PaymentFailedEvent
            {
                PaymentAttemptReference = paymentAttemptRef,
                Reason = errorMessage,
                Timestamp = DateTimeOffset.UtcNow
            };

            try
            {
                await _messagePublisher.PublishPaymentFailedEventAsync(failedEvent);
            }
            catch (Exception pubEx)
            {
                _logger.LogError(pubEx, "CRITICAL: Failed to publish PaymentFailedEvent after a payment error (PaymentAttemptRef: {PaymentAttemptRef}).", paymentAttemptRef);
            }
        }

        public async Task<CreatePaymentResponse> Create(CreatePaymentRequest request)
        {
            ValidatePaymentRequest(request);

            string paymentMethodId;

            try
            {
                _logger.LogInformation("Creating Stripe PaymentMethod from provided card details.");
                var paymentMethodOptions = new PaymentMethodCreateOptions
                {
                    Type = "card",
                    Card = new PaymentMethodCardOptions { Token = request.Token },
                    BillingDetails = new PaymentMethodBillingDetailsOptions { Name = request.CardholderName }
                };
                var paymentMethod = await _paymentMethodService.CreateAsync(paymentMethodOptions);
                paymentMethodId = paymentMethod.Id;
                _logger.LogInformation($"Successfully created PaymentMethod with ID: {paymentMethodId}");
            }
            catch (StripeException e)
            {
                var error = $"Stripe API error during PaymentMethod creation: {e.Message}";
                await HandlePaymentErrorAsync(request.Token, error, e);
                throw new InvalidOperationException(error, e);
            }
            catch (Exception e)
            {
                var error = $"An unexpected server error occurred during PaymentMethod creation: {e.Message}";
                await HandlePaymentErrorAsync(request.Token, error, e);
                throw new InvalidOperationException(error, e);
            }

            PaymentIntent paymentIntent = null;
            try
            {
                var paymentIntentOptions = new PaymentIntentCreateOptions
                {
                    Amount = request.Amount,
                    Currency = request.Currency.ToLowerInvariant(),
                    PaymentMethod = paymentMethodId,
                    Confirm = true,
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true,
                        AllowRedirects = "never"
                    }
                }; 
                _logger.LogInformation($"Calling Stripe API to create and confirm PaymentIntent for Amount: {request.Amount} {request.Currency}");
                paymentIntent = await _paymentIntentService.CreateAsync(paymentIntentOptions);
                _logger.LogInformation($"Stripe PaymentIntent created with ID: {paymentIntent.Id}, Status: {paymentIntent.Status}");

                if (paymentIntent.Status == "succeeded" || paymentIntent.Status == "requires_capture")
                {
                    var succeededEvent = new PaymentSucceededEvent
                    {
                        PaymentIntentId = paymentIntent.Id,
                        Amount = paymentIntent.Amount,
                        Currency = paymentIntent.Currency,
                        Timestamp = DateTimeOffset.UtcNow
                    };
                    await _messagePublisher.PublishPaymentSucceededEventAsync(succeededEvent);
                    _logger.LogInformation($"Payment Succeeded. Published PaymentSucceededEvent for PaymentIntentId {paymentIntent.Id}.");

                    return new CreatePaymentResponse
                    {
                        PaymentIntentId = paymentIntent.Id,
                        Amount = paymentIntent.Amount,
                        Currency = paymentIntent.Currency,
                        Status = paymentIntent.Status 
                    };
                } 
                var errorReason = $"Payment not successful. Stripe PaymentIntent Status: {paymentIntent.Status}.";
                
                if (paymentIntent.LastPaymentError != null)
                {
                    errorReason += $" Stripe Error: {paymentIntent.LastPaymentError.Message} (Type: {paymentIntent.LastPaymentError.Type}, Code: {paymentIntent.LastPaymentError.Code})";
                }
                await HandlePaymentErrorAsync(paymentIntent.Id, errorReason);
                throw new InvalidOperationException(errorReason);
            }
            catch (StripeException e)
            {
                var error = $"Stripe API error during PaymentIntent operation: {e.Message}";
                var idToLog = paymentIntent?.Id ?? $"PM:{paymentMethodId}"; 
                await HandlePaymentErrorAsync(idToLog, error, e);
                throw new InvalidOperationException(error, e);
            }
            catch (Exception e) 
            {
                var error = $"An unexpected server error occurred during PaymentIntent operation: {e.Message}";
                var idToLog = paymentIntent?.Id ?? $"PM:{paymentMethodId}";
                await HandlePaymentErrorAsync(idToLog, error, e);
                throw new InvalidOperationException(error, e);
            }
        }
    }
}