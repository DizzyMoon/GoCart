using System;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace payment.Messaging
{
    public class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
    {
        private readonly ILogger<RabbitMqPublisher> _logger;
        private readonly IRabbitMqConnectionManager _connectionManager;
        private IModel _channel;

        public RabbitMqPublisher(IRabbitMqConnectionManager connectionManager, ILogger<RabbitMqPublisher> logger)
        {
            _connectionManager = connectionManager;
            _logger = logger;
            
            _channel = _connectionManager.CreateChannel();
            _logger.LogInformation("RabbitMqPublisher initialized and channel obtained.");
        }


        public void PublishPaymentSuccess(string paymentIntentId, long amount, string currency)
        {
            if (_channel == null || !_channel.IsOpen)
            {
                _logger.LogError("RabbitMQ channel is not open. Cannot publish payment success message. Attempting to recreate channel.");
                try
                {
                    _channel = _connectionManager.CreateChannel();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to recreate RabbitMQ channel for publisher.");
                    return;
                }

                if (_channel == null || !_channel.IsOpen)
                {
                    _logger.LogError("Failed to obtain a valid RabbitMQ channel after retry. Cannot publish payment success message.");
                    return;
                }
            }

            var message = new
            {
                PaymentIntentId = paymentIntentId,
                Amount = amount,
                Currency = currency,
                Timestamp = DateTimeOffset.UtcNow
            };
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            _channel.BasicPublish(
                exchange: "payment_exchange",
                routingKey: "payment.success",
                basicProperties: null,
                body: body);

            _logger.LogInformation($"Published payment success message for PaymentIntent {paymentIntentId}");
        }

        public void PublishPaymentFailedToOrder(string paymentIntentId, string reason)
        {
            if (_channel == null || !_channel.IsOpen)
            {
                _logger.LogError("RabbitMQ channel is not open. Cannot publish payment failure message. Attempting to recreate channel.");
                try
                {
                    _channel = _connectionManager.CreateChannel();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to recreate RabbitMQ channel for publisher.");
                    return;
                }

                if (_channel == null || !_channel.IsOpen)
                {
                    _logger.LogError("Failed to obtain a valid RabbitMQ channel after retry. Cannot publish payment failure message.");
                    return;
                }
            }

            var message = new
            {
                PaymentIntentId = paymentIntentId,
                Reason = reason,
                Timestamp = DateTimeOffset.UtcNow
            };
            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            _channel.BasicPublish(
                exchange: "payment_exchange",
                routingKey: "payment.failure",
                basicProperties: null,
                body: body);

            _logger.LogInformation($"Published payment failure message for PaymentIntent {paymentIntentId}");
        }

        public void Dispose()
        {
            _logger.LogInformation("Disposing RabbitMqPublisher's channel.");
            if (_channel != null && _channel.IsOpen)
            {
                _channel.Close();
                _channel.Dispose();
            }
            GC.SuppressFinalize(this);
        }
    }
}