using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace payment.Messaging
{
    public class RabbitMqPaymentFailureConsumer : BackgroundService
    {
        private readonly ILogger<RabbitMqPaymentFailureConsumer> _logger;
        private readonly IRabbitMqPublisher _rabbitMqPublisher;
        private readonly IRabbitMqConnectionManager _connectionManager;
        private IModel _channel;

        private const int MaxRetries = 3;
        private const int RetryDelayMs = 5000;

        public RabbitMqPaymentFailureConsumer(
            IRabbitMqConnectionManager connectionManager,
            ILogger<RabbitMqPaymentFailureConsumer> logger,
            IRabbitMqPublisher rabbitMqPublisher)
        {
            _connectionManager = connectionManager;
            _logger = logger;
            _rabbitMqPublisher = rabbitMqPublisher;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();

            try
            {
                _channel = _connectionManager.CreateChannel();
                _logger.LogInformation("RabbitMqPaymentFailureConsumer initialized with channel.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize RabbitMQ channel for consumer. Consumer will not start.");
                return;
            }


            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                if (_channel == null || !_channel.IsOpen)
                {
                    _logger.LogWarning("RabbitMQ channel closed during message reception. Re-creating channel and deferring message.");
                    try
                    {
                        _channel = _connectionManager.CreateChannel();
                        _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to re-create channel. Message will be lost or stuck.");
                        _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                    }
                    return;
                }

                var body = ea.Body.ToArray();
                var messageString = Encoding.UTF8.GetString(body);
                PaymentFailedMessage message = null;

                try
                {
                    message = JsonSerializer.Deserialize<PaymentFailedMessage>(messageString);
                    if (message == null)
                    {
                        _logger.LogError($"Received malformed message on '{ea.RoutingKey}': {messageString}. Skipping.");
                        _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                        return;
                    }

                    _logger.LogInformation($"Received payment failure message for PaymentIntent: {message.PaymentIntentId}, Reason: {message.Reason}, RetryCount: {message.RetryCount}");

                    if (message.RetryCount < MaxRetries)
                    {
                        _logger.LogInformation($"Attempting retry for PaymentIntent: {message.PaymentIntentId}. Current retry count: {message.RetryCount}");
                        await Task.Delay(RetryDelayMs, stoppingToken);
                        
                        bool retrySuccess = new Random().Next(100) > 50;

                        if (retrySuccess)
                        {
                            _logger.LogInformation($"Retry successful for PaymentIntent: {message.PaymentIntentId}");
                            long dummyAmount = 10000;
                            string dummyCurrency = "USD";

                            _rabbitMqPublisher.PublishPaymentSuccess(message.PaymentIntentId, dummyAmount, dummyCurrency);
                            _channel.BasicAck(ea.DeliveryTag, multiple: false);
                        }
                        else
                        {
                            _logger.LogWarning($"Retry failed for PaymentIntent: {message.PaymentIntentId}. Incrementing retry count.");
                            message.RetryCount++;
                            var updatedBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
                            _channel.BasicPublish(
                                exchange: "payment_exchange",
                                routingKey: "payment.failure",
                                basicProperties: null,
                                body: updatedBody);
                            _channel.BasicAck(ea.DeliveryTag, multiple: false);
                        }
                    }
                    else
                    {
                        _logger.LogError($"Max retries ({MaxRetries}) reached for PaymentIntent: {message.PaymentIntentId}. Moving to dead-letter or manual review.");
                        _channel.BasicAck(ea.DeliveryTag, multiple: false);
                    }
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, $"JSON deserialization error for message: {messageString}. Message will be dropped.");
                    _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation($"Payment failure processing for {message?.PaymentIntentId ?? "Unknown"} cancelled.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error processing payment failure message for PaymentIntent: {message?.PaymentIntentId ?? "Unknown"}. Message will be requeued.");
                    _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true); // Nack and requeue on unexpected errors
                }
            };

            // Start consuming messages
            _channel.BasicConsume(queue: "order_payment_failure_queue", autoAck: false, consumer: consumer);

            _logger.LogInformation("RabbitMQ Payment Failure Consumer started. Listening for messages...");

            // This ensures the hosted service keeps running until the application is stopped
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        // Dispose method for cleaning up the channel
        public override void Dispose()
        {
            _logger.LogInformation("Disposing RabbitMqPaymentFailureConsumer's channel.");
            if (_channel != null && _channel.IsOpen)
            {
                _channel.Close();
                _channel.Dispose();
            }
            // The underlying IConnection is managed and disposed by RabbitMqConnectionManager
            base.Dispose(); // Call the base class Dispose method
        }
    }
}