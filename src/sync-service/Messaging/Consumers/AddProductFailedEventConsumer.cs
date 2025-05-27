using sync_service.Messaging.Connection;
using sync_service.Messaging.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using sync_service.ProductServices;

namespace sync_service.Messaging.Consumers
{
    public class AddProductFailedEventConsumer : BackgroundService
    {
        private readonly ILogger<AddProductFailedEventConsumer> _logger;
        private readonly IRabbitMqConnectionManager _connectionManager;
        private readonly IServiceProvider _serviceProvider; // To resolve scoped IOrderService
        private IModel _channel;
        private const string QueueName = "add_product_failed_event_queue"; // As declared in RabbitMqConnectionManager

        public AddProductFailedEventConsumer(
            ILogger<AddProductFailedEventConsumer> logger,
            IRabbitMqConnectionManager connectionManager,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _connectionManager = connectionManager;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AddProductFailedEventConsumer is starting.");
            stoppingToken.Register(() => _logger.LogInformation("AddProductFailedEventConsumer is stopping."));

            while (!stoppingToken.IsCancellationRequested)
            {
                if (!_connectionManager.IsConnected)
                {
                    _logger.LogWarning(
                        "AddProductFailedEventConsumer: RabbitMQ not connected. Retrying connection in 5 seconds...");
                    await Task.Delay(5000, stoppingToken); // Wait before retrying connection
                    _connectionManager.TryConnect(); // Attempt to reconnect
                    continue; // Re-check connection status in the next loop iteration
                }

                try
                {
                    _channel?.Dispose(); // Dispose previous channel if any due to restart/reconnection
                    _channel = _connectionManager.CreateChannel();
                    _logger.LogInformation("AddProductFailedEventConsumer: Channel created/recreated.");

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
                            "AddProductFailedEventConsumer: Received message. DeliveryTag: {DeliveryTag}. Message: {MessageBody}",
                            ea.DeliveryTag, messageBody);

                        AddProductFailedEvent? addProductEvent = null;
                        try
                        {
                            addProductEvent = JsonSerializer.Deserialize<AddProductFailedEvent>(messageBody);
                            if (addProductEvent == null)
                            {
                                _logger.LogError(
                                    "AddProductFailedEventConsumer: Failed to deserialize AddProductFailedEvent or event is null. Message: {MessageBody}",
                                    messageBody);
                                _channel.BasicNack(ea.DeliveryTag, false, false); // Send to DLX
                                return;
                            }

                            // Create a scope to resolve scoped services like IOrderService
                            using (var scope = _serviceProvider.CreateScope())
                            {
                                var orderService = scope.ServiceProvider.GetRequiredService<IProductService>();
                                await orderService.ProcessFailedAddProductEventAsync(addProductEvent);
                            }

                            _channel.BasicAck(ea.DeliveryTag, false); // Acknowledge after successful processing
                            _logger.LogInformation(
                                "AddProductFailedEventConsumer: AddProductFailedEvent processed. DeliveryTag: {DeliveryTag}",
                                ea.DeliveryTag);
                        }
                        catch (JsonException jsonEx)
                        {
                            _logger.LogError(jsonEx,
                                "AddProductFailedEventConsumer: JSON Deserialization error. DeliveryTag: {DeliveryTag}. Message: {MessageBody}. Sending to DLX.",
                                ea.DeliveryTag, messageBody);
                            _channel.BasicNack(ea.DeliveryTag, false, false); // To DLX
                        }
                        catch (Exception
                               ex) // Catch exceptions from IOrderService.ProcessFailedAddProductEventAsync or other issues
                        {
                            _logger.LogError(ex,
                                "AddProductFailedEventConsumer: Error processing AddProductFailedEvent for AddProductAttemptReference {ProductName}. DeliveryTag: {DeliveryTag}. Message: {MessageBody}. Sending to DLX.",
                                addProductEvent?.Name, ea.DeliveryTag, messageBody);
                            _channel.BasicNack(ea.DeliveryTag, false, false); // To DLX
                        }
                    };

                    _channel.ModelShutdown += (sender, args) =>
                    {
                        _logger.LogWarning(
                            "AddProductFailedEventConsumer: Channel shutdown. Reason: {ReplyText}. Will attempt to re-establish.",
                            args.ReplyText);
                        // The outer loop in ExecuteAsync will handle re-establishing the channel.
                    };

                    _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);
                    _logger.LogInformation(
                        "AddProductEventConsumer: Subscribed to queue '{QueueName}'. Waiting for messages.",
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
                        "AddProductFailedEventConsumer: Exception in ExecuteAsync main loop. Retrying in 10 seconds...");
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

            _logger.LogInformation("AddProductFailedEventConsumer has stopped.");
        }

        public override void Dispose()
        {
            _logger.LogInformation("Disposing AddProductFailedEventConsumer.");
            _channel?.Close(); // Ensure channel is closed on dispose
            _channel?.Dispose();
            base.Dispose();
        }
    }
}