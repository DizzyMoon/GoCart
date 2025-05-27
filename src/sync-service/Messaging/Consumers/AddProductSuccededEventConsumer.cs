using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using product.Messaging.Connection;
using sync_service.Messaging.Connection;
using sync_service.Messaging.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Product.ProductModels;
using sync_service.ProductServices;

namespace sync_service.Messaging.Consumers
{
    public class AddProductSucceededEventConsumer : BackgroundService
    {
        private readonly ILogger<AddProductSucceededEventConsumer> _logger;
        private readonly IRabbitMqConnectionManager _connectionManager;
        private readonly IServiceProvider _serviceProvider; // To resolve scoped services like DbContext via IOrderService
        private IModel _channel;
        private const string QueueName = "add_product_succeeded_event_queue";

        public AddProductSucceededEventConsumer(
            ILogger<AddProductSucceededEventConsumer> logger,
            IRabbitMqConnectionManager connectionManager,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _connectionManager = connectionManager;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AddProductSucceededEventConsumer is starting.");
            stoppingToken.Register(() => _logger.LogInformation("AddProductSucceededEventConsumer is stopping."));

            // Loop to ensure connection and channel are established, especially on startup
            while (!stoppingToken.IsCancellationRequested)
            {
                if (!_connectionManager.IsConnected)
                {
                    _logger.LogWarning("RabbitMQ not connected. Retrying connection in 5 seconds...");
                    await Task.Delay(5000, stoppingToken); // Wait before retrying connection
                    _connectionManager.TryConnect(); // Attempt to reconnect
                    continue; // Re-check connection status in the next loop iteration
                }

                try
                {
                    _channel?.Dispose(); // Dispose previous channel if any
                    _channel = _connectionManager.CreateChannel();
                    _logger.LogInformation("Channel created for {ConsumerName}.", nameof(AddProductSucceededEventConsumer));
                    
                    // Ensure queue is declared (idempotent). This is often done centrally at startup by calling
                    // _connectionManager.DeclareQueuesAndBindings();
                    // but can be re-asserted here if desired, or assumed to be done.

                    _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false); // Process one message at a time

                    var consumer = new AsyncEventingBasicConsumer(_channel);
                    consumer.Received += async (sender, ea) =>
                    {
                        var messageBody = Encoding.UTF8.GetString(ea.Body.ToArray());
                        _logger.LogInformation("Received AddProductSucceededEvent. DeliveryTag: {DeliveryTag}. Message: {MessageBody}", ea.DeliveryTag, messageBody);

                        try
                        {
                            var product = JsonSerializer.Deserialize<ProductModel>(messageBody);
                            if (product == null)
                            {
                                _logger.LogError("Failed to deserialize AddProductSucceededEvent or event is null. Message: {MessageBody}", messageBody);
                                _channel.BasicNack(ea.DeliveryTag, false, false); // To DLX
                                return;
                            }

                            // Create a scope to resolve scoped services like IOrderService (and its DbContext)
                            using (var scope = _serviceProvider.CreateScope())
                            {
                                var orderService = scope.ServiceProvider.GetRequiredService<IProductService>();
                                await orderService.createProduct(product);
                            }
                            
                            _channel.BasicAck(ea.DeliveryTag, false); // Acknowledge after successful processing
                            _logger.LogInformation("PaymentSucceededEvent processed successfully. DeliveryTag: {DeliveryTag}", ea.DeliveryTag);
                        }
                        catch (JsonException jsonEx)
                        {
                            _logger.LogError(jsonEx, "JSON Deserialization error for PaymentSucceededEvent. DeliveryTag: {DeliveryTag}. Message: {MessageBody}. Sending to DLX.", ea.DeliveryTag, messageBody);
                            _channel.BasicNack(ea.DeliveryTag, false, false); // To DLX
                        }
                        catch (Exception ex) // Catch exceptions from IOrderService.CreateOrderFromSuccessfulPaymentAsync or other issues
                        {
                            _logger.LogError(ex, "Error processing PaymentSucceededEvent. DeliveryTag: {DeliveryTag}. Message: {MessageBody}. Sending to DLX.", ea.DeliveryTag, messageBody);
                            _channel.BasicNack(ea.DeliveryTag, false, false); // To DLX
                        }
                    };
                    
                    _channel.ModelShutdown += (sender, args) => {
                        _logger.LogWarning("Channel shutdown for {ConsumerName}. Reason: {ReplyText}. Will attempt to re-establish.", nameof(AddProductSucceededEventConsumer), args.ReplyText);
                    };

                    _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);
                    _logger.LogInformation("Subscribed to queue '{QueueName}'. Waiting for messages.", QueueName);

                    // Keep the consumer alive while not cancelled
                    while (!stoppingToken.IsCancellationRequested && _channel.IsOpen)
                    {
                        await Task.Delay(1000, stoppingToken); // Check periodically
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception in AddProductSucceededEventConsumer ExecuteAsync. Retrying in 10 seconds...");
                    await Task.Delay(10000, stoppingToken); // Wait before retrying the whole setup
                }
                finally
                {
                     _channel?.Close(); // Ensure channel is closed if loop exits
                }
            }
            _logger.LogInformation("AddProductSucceededEventConsumer has stopped.");
        }

        public override void Dispose()
        {
            _logger.LogInformation("Disposing AddProductSucceededEventConsumer.");
            _channel?.Close();
            _channel?.Dispose();
            base.Dispose();
        }
    }
}
