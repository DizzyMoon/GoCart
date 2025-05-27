using order.Messaging.Connection;
using order.Messaging.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Order.OrderService;

namespace order.Messaging.Consumers
{
    public class PaymentFailedEventConsumer : BackgroundService
    {
        private readonly ILogger<PaymentFailedEventConsumer> _logger;
        private readonly IRabbitMqConnectionManager _connectionManager;
        private readonly IServiceProvider _serviceProvider; // To resolve scoped IOrderService
        private IModel _channel;
        private const string QueueName = "order_payment_failed_event_queue"; // As declared in RabbitMqConnectionManager

        public PaymentFailedEventConsumer(
            ILogger<PaymentFailedEventConsumer> logger,
            IRabbitMqConnectionManager connectionManager,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _connectionManager = connectionManager;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PaymentFailedEventConsumer is starting.");
            stoppingToken.Register(() => _logger.LogInformation("PaymentFailedEventConsumer is stopping."));

            while (!stoppingToken.IsCancellationRequested)
            {
                if (!_connectionManager.IsConnected)
                {
                    _logger.LogWarning(
                        "PaymentFailedEventConsumer: RabbitMQ not connected. Retrying connection in 5 seconds...");
                    await Task.Delay(5000, stoppingToken); // Wait before retrying connection
                    _connectionManager.TryConnect(); // Attempt to reconnect
                    continue; // Re-check connection status in the next loop iteration
                }

                try
                {
                    _channel?.Dispose(); // Dispose previous channel if any due to restart/reconnection
                    _channel = _connectionManager.CreateChannel();
                    _logger.LogInformation("PaymentFailedEventConsumer: Channel created/recreated.");

                    // Ensure queue is declared (idempotent). This is often done centrally at startup by calling
                    // _connectionManager.DeclareQueuesAndBindings();
                    // but can be re-asserted here if desired.

                    _channel.BasicQos(prefetchSize: 0, prefetchCount: 1,
                        global: false); // Process one message at a time

                    var consumer = new AsyncEventingBasicConsumer(_channel);
                    consumer.Received += async (sender, ea) =>
                    {
                        var messageBody = Encoding.UTF8.GetString(ea.Body.ToArray());
                        _logger.LogInformation(
                            "PaymentFailedEventConsumer: Received message. DeliveryTag: {DeliveryTag}. Message: {MessageBody}",
                            ea.DeliveryTag, messageBody);

                        PaymentFailedEvent? paymentEvent = null;
                        try
                        {
                            paymentEvent = JsonSerializer.Deserialize<PaymentFailedEvent>(messageBody);
                            if (paymentEvent == null)
                            {
                                _logger.LogError(
                                    "PaymentFailedEventConsumer: Failed to deserialize PaymentFailedEvent or event is null. Message: {MessageBody}",
                                    messageBody);
                                _channel.BasicNack(ea.DeliveryTag, false, false); // Send to DLX
                                return;
                            }

                            // Create a scope to resolve scoped services like IOrderService
                            using (var scope = _serviceProvider.CreateScope())
                            {
                                var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
                                await orderService.ProcessFailedPaymentEventAsync(paymentEvent);
                            }

                            _channel.BasicAck(ea.DeliveryTag, false); // Acknowledge after successful processing
                            _logger.LogInformation(
                                "PaymentFailedEventConsumer: PaymentFailedEvent processed. DeliveryTag: {DeliveryTag}",
                                ea.DeliveryTag);
                        }
                        catch (JsonException jsonEx)
                        {
                            _logger.LogError(jsonEx,
                                "PaymentFailedEventConsumer: JSON Deserialization error. DeliveryTag: {DeliveryTag}. Message: {MessageBody}. Sending to DLX.",
                                ea.DeliveryTag, messageBody);
                            _channel.BasicNack(ea.DeliveryTag, false, false); // To DLX
                        }
                        catch (Exception
                               ex) // Catch exceptions from IOrderService.ProcessFailedPaymentEventAsync or other issues
                        {
                            _logger.LogError(ex,
                                "PaymentFailedEventConsumer: Error processing PaymentFailedEvent for PaymentAttemptReference {PaymentAttemptRef}. DeliveryTag: {DeliveryTag}. Message: {MessageBody}. Sending to DLX.",
                                paymentEvent?.PaymentAttemptReference, ea.DeliveryTag, messageBody);
                            _channel.BasicNack(ea.DeliveryTag, false, false); // To DLX
                        }
                    };

                    _channel.ModelShutdown += (sender, args) =>
                    {
                        _logger.LogWarning(
                            "PaymentFailedEventConsumer: Channel shutdown. Reason: {ReplyText}. Will attempt to re-establish.",
                            args.ReplyText);
                        // The outer loop in ExecuteAsync will handle re-establishing the channel.
                    };

                    _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);
                    _logger.LogInformation(
                        "PaymentFailedEventConsumer: Subscribed to queue '{QueueName}'. Waiting for messages.",
                        QueueName);

                    // Keep the consumer alive by waiting until cancellation is requested or channel closes
                    while (!stoppingToken.IsCancellationRequested && _channel.IsOpen)
                    {
                        await Task.Delay(1000, stoppingToken); // Check periodically
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "PaymentFailedEventConsumer: Exception in ExecuteAsync main loop. Retrying in 10 seconds...");
                    await Task.Delay(10000, stoppingToken); // Wait before retrying the whole setup
                }
                finally
                {
                    // Ensure channel is closed if the loop exits for any reason other than normal shutdown
                    if (_channel?.IsOpen == true)
                    {
                        _channel.Close();
                    }
                }
            }

            _logger.LogInformation("PaymentFailedEventConsumer has stopped.");
        }

        public override void Dispose()
        {
            _logger.LogInformation("Disposing PaymentFailedEventConsumer.");
            _channel?.Close(); // Ensure channel is closed on dispose
            _channel?.Dispose();
            base.Dispose();
        }
    }
}