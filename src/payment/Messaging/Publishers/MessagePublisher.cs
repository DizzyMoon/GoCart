using payment.Messaging.Connection;
using payment.Messaging.Events;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace payment.Messaging.Publishers
{
    public class MessagePublisher : IMessagePublisher, IDisposable
    {
        private readonly IRabbitMqConnectionManager _connectionManager;
        private readonly ILogger<MessagePublisher> _logger;
        private IModel _channel;
        private readonly object _channelLock = new object(); // For thread-safe channel creation

        private const string PaymentEventsExchangeName = "payment_events_exchange";

        public MessagePublisher(IRabbitMqConnectionManager connectionManager, ILogger<MessagePublisher> logger)
        {
            _connectionManager = connectionManager;
            _logger = logger;
            EnsureChannelIsAvailable(); // Try to create channel on initialization
        }

        private void EnsureChannelIsAvailable()
        {
            lock (_channelLock) // Ensure thread safety for channel access/creation
            {
                if (_channel != null && _channel.IsOpen)
                {
                    return; // Channel is already open and fine
                }

                _logger.LogInformation("Attempting to ensure RabbitMQ channel is available.");
                if (!_connectionManager.IsConnected)
                {
                    _logger.LogWarning("RabbitMQ connection not established. Attempting to connect via ConnectionManager.");
                    if (!_connectionManager.TryConnect()) // TryConnect should handle its own retries
                    {
                        _logger.LogError("RabbitMqMessagePublisher: Could not connect via ConnectionManager. Channel cannot be created.");
                        // Not throwing here, to allow application to start, but publish will fail.
                        return;
                    }
                }
                
                try
                {
                    _channel?.Dispose(); // Dispose old channel if it exists and is closed/faulted
                    _channel = _connectionManager.CreateChannel();
                    _channel.ConfirmSelect(); // Enable publisher confirms for reliability
                    // Add event handlers for channel issues if needed (e.g., _channel.ModelShutdown)
                    _logger.LogInformation("RabbitMqMessagePublisher: Channel created/recreated and publisher confirms enabled.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "RabbitMqMessagePublisher: Failed to create RabbitMQ channel.");
                    _channel = null; // Ensure channel is null if creation failed
                }
            }
        }
        
        private async Task PublishAsync<T>(T eventData, string exchangeName, string routingKey, string eventTypeForLogging) where T : class
        {
            EnsureChannelIsAvailable(); // Ensure channel is ready before each publish

            if (_channel == null || !_channel.IsOpen)
            {
                _logger.LogError("Cannot publish {EventType}: RabbitMQ channel is not available.", eventTypeForLogging);
                // In a real-world scenario, you might throw an exception or implement a retry mechanism with a dead-letter strategy for publish failures.
                // For this demo, we'll log and not throw to prevent immediate request failure if RabbitMQ is temporarily down.
                // However, this means the message is lost if not handled further up.
                throw new InvalidOperationException($"Cannot publish {eventTypeForLogging}: RabbitMQ channel is not available.");
            }

            var messageBody = JsonSerializer.Serialize(eventData);
            var body = Encoding.UTF8.GetBytes(messageBody);

            try
            {
                // Consider making properties persistent for critical messages
                var properties = _channel.CreateBasicProperties();
                properties.Persistent = true; // Ensure messages survive broker restart

                _logger.LogInformation("Publishing {EventType} to exchange '{Exchange}' with routing key '{RoutingKey}'. Body: {Body}",
                    eventTypeForLogging, exchangeName, routingKey, messageBody);

                _channel.BasicPublish(
                    exchange: exchangeName,
                    routingKey: routingKey,
                    mandatory: true, // If true, message is returned to publisher if it can't be routed. Requires BasicReturn handler.
                    basicProperties: properties,
                    body: body);
                
                // WaitForConfirms makes publishing effectively synchronous for the confirmation.
                // For high throughput, consider async confirms or batching.
                if (_channel.WaitForConfirms(TimeSpan.FromSeconds(5))) 
                {
                     _logger.LogInformation("{EventType} published and confirmed successfully by broker.", eventTypeForLogging);
                }
                else
                {
                    _logger.LogWarning("{EventType} publish not confirmed by broker within timeout. Message might not have been durably queued.", eventTypeForLogging);
                    // This is a critical situation. The message might be lost or not processed.
                    // Implement more robust error handling/retry or compensating action here.
                    throw new TimeoutException($"Publish confirmation timeout for {eventTypeForLogging}.");
                }
                // For truly async: await Task.Run(() => _channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing {EventType}. Exchange: {Exchange}, RoutingKey: {RoutingKey}, Body: {Body}",
                    eventTypeForLogging, exchangeName, routingKey, messageBody);
                // Re-throw to indicate publish failure to the caller
                throw; 
            }
        }

        public async Task PublishPaymentSucceededEventAsync(PaymentSucceededEvent eventData)
        {
            await PublishAsync(eventData, PaymentEventsExchangeName, "payment.succeeded", nameof(PaymentSucceededEvent));
        }

        public async Task PublishPaymentFailedEventAsync(PaymentFailedEvent eventData)
        {
            await PublishAsync(eventData, PaymentEventsExchangeName, "payment.failed", nameof(PaymentFailedEvent));
        }
        
        public void Dispose()
        {
            _logger.LogInformation("Disposing RabbitMqMessagePublisher's channel.");
            try
            {
                _channel?.Close(); // Close before dispose
                _channel?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing publisher channel.");
            }
        }
    }
}