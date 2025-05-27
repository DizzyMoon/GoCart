using order.Messaging.Events;
using Order.OrderModels;
using Order.OrderRepository;
using Order.OrderService;

namespace order.OrderService
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly ILogger<OrderService> _logger;

        public OrderService(IOrderRepository orderRepository, ILogger<OrderService> logger)
        {
            _orderRepository = orderRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<OrderModel>> GetQueryCollection()
        {
            _logger.LogInformation("OrderService: Fetching all orders.");
            return await _orderRepository.GetQueryCollection();
        }

        public async Task<OrderModel?> Get(int orderId)
        {
            _logger.LogInformation("OrderService: Fetching order with ID {OrderId}.", orderId);
            var order = await _orderRepository.Get(orderId);

            if (order == null)
            {
                _logger.LogWarning("OrderService: Order with ID {OrderId} not found.", orderId);
                throw new KeyNotFoundException($"Order with ID {orderId} not found.");
            }

            return order;
        }

        public async Task<OrderModel> Delete(int orderId)
        {
            _logger.LogInformation("OrderService: Attempting to delete order with ID {OrderId}.", orderId);
            var orderToDelete = await _orderRepository.Delete(orderId);

            if (orderToDelete == null)
            {
                _logger.LogWarning("OrderService: Order with ID {OrderId} not found for deletion.", orderId);
                throw new KeyNotFoundException($"Order with ID {orderId} not found.");
            }

            _logger.LogInformation("OrderService: Successfully deleted order with ID {OrderId}.", orderId);
            return orderToDelete;
        }

        private string GenerateOrderNumber()
        {
            return Guid.NewGuid().ToString("D").ToUpper();
        }

        public async Task<OrderModel?> ProcessSuccessfulPaymentEventAsync(PaymentSucceededEvent paymentSucceededEvent)
        {
            if (paymentSucceededEvent == null)
            {
                _logger.LogError("OrderService: ProcessSuccessfulPaymentEventAsync received a null paymentEvent.");
                throw new ArgumentNullException(nameof(paymentSucceededEvent));
            }
            
            _logger.LogInformation($"OrderService: Processing PaymentSucceededEvent for PaymentIntentId: {paymentSucceededEvent.PaymentIntentId}, Amount: {paymentSucceededEvent.Amount} {paymentSucceededEvent.Currency}");
            
            // Idempotency Check
            OrderModel? existingOrder = null;
            try
            {
                existingOrder = await _orderRepository.GetByPaymentIntentIdAsync(paymentSucceededEvent.PaymentIntentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"OrderService: Error checking for existing order with PaymentIntentId {paymentSucceededEvent.PaymentIntentId} during idempotency check.");
                throw;
            }

            if (existingOrder != null)
            {
                _logger.LogWarning($"OrderService: Order already exists for PaymentIntentId: {paymentSucceededEvent.PaymentIntentId}. Existing OrderNumber: {existingOrder.OrderNumber}. Skipping creation.");
                return existingOrder;
            }

            var newOrder = new OrderModel
            {
                OrderNumber = GenerateOrderNumber(),
                OrderDate = DateTime.UtcNow,
                PaymentIntentId = paymentSucceededEvent.PaymentIntentId,
                Status = "PROCESSING_ORDER"
            };

            try
            {
                var createdOrder = await _orderRepository.Create(newOrder);
                _logger.LogInformation(
                    $"OrderService: Successfully created order from PaymentSucceededEvent. OrderNumber: {createdOrder.OrderNumber}, PaymentIntentId: {createdOrder.PaymentIntentId}, Status: {createdOrder.Status}");
                return createdOrder;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"OrderService: Database error creating order from PaymentSucceededEvent for PaymentIntentId {paymentSucceededEvent.PaymentIntentId}.");
                throw;
            }
        }

        public async Task ProcessFailedPaymentEventAsync(PaymentFailedEvent paymentFailedEvent)
        {
            if (paymentFailedEvent == null)
            {
                _logger.LogError("OrderService: ProcessFailedPaymentEventAsync received a null paymentEvent.");
                throw new ArgumentNullException(nameof(paymentFailedEvent));
            }
            
            _logger.LogWarning("OrderService: Received PaymentFailedEvent. PaymentAttemptReference: {PaymentAttemptRef}, PaymentIntentId: {PaymentIntentId}, Reason: {Reason}",
                paymentFailedEvent.PaymentAttemptReference, paymentFailedEvent.PaymentIntentId ?? "N/A", paymentFailedEvent.Reason);

            await Task.CompletedTask;
        }
    }
}