using System;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace payment.Messaging
{
    public class RabbitMqPublisher : IRabbitMqPublisher, IDisposable
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RabbitMqPublisher> _logger;
        private IConnection _connection;
        private IModel _channel;

        public RabbitMqPublisher(IConfiguration configuration, ILogger<RabbitMqPublisher> logger)
        {
            _configuration = configuration;
            _logger = logger;
            InitializeRabbitMq();
        }

        private void InitializeRabbitMq()
        {
            try
            {
                var rabbitMQUser = _configuration["RABBITMQ_USER"] ?? "guest";
                var rabbitMQPassword = _configuration["RABBITMQ_PASSWORD"] ?? "guest";
                var rabbitMQHost = _configuration["RABBITMQ_HOST"] ?? "rabbitmq";

                var factory = new ConnectionFactory()
                {
                    HostName = rabbitMQHost,
                    UserName = rabbitMQUser,
                    Password = rabbitMQPassword,
                    Port = AmqpTcpEndpoint.DefaultAmqpSslPort,
                    DispatchConsumersAsync = true
                };

                int retries = 5;
                int delayMS = 2000;
                for (int i = 0; i < retries; i++)
                {
                    try
                    {
                        _connection = factory.CreateConnection();
                        _channel = _connection.CreateModel();
                        break;
                    }
                    catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException ex)
                    {
                        _logger.LogWarning(
                            $"Attempt {i + 1}/{retries}: Could not connect to RabbitMQ at {rabbitMQHost}. Retrying in {delayMS / 1000} seconds. Error: {ex.Message}");
                        System.Threading.Thread.Sleep(delayMS);
                    }
                }

                if (_connection == null || !_connection.IsOpen)
                {
                    throw new InvalidOperationException($"Failed to connect to RabbitMQ after {retries} retries.");
                }

                // Exchanges and queues
                _channel.ExchangeDeclare(exchange: "payment_exchange", type: ExchangeType.Direct);

                _channel.QueueDeclare(queue: "order_payment_success_queue", durable: true, exclusive: false,
                    autoDelete: false, arguments: null);
                _channel.QueueDeclare(queue: "order_payment_failure_queue", durable: true, exclusive: false,
                    autoDelete: false, arguments: null);
                _channel.QueueDeclare(queue: "payment_notification_queue", durable: true, exclusive: false,
                    autoDelete: false, arguments: null);

                _channel.QueueBind(queue: "order_payment_success_queue", exchange: "payment_exchange",
                    routingKey: "payment.success");
                _channel.QueueBind(queue: "order_payment_failure_queue", exchange: "payment_exchange",
                    routingKey: "payment.failure");
                _channel.QueueBind(queue: "payment_notification_queue", exchange: "payment_exchange",
                    routingKey: "payment.notification");

                _logger.LogInformation("Successfully connected to RabbitMQ and declared queues.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal: Could not initialize RabbitMQ connection or declarations.");
                throw;
            }
        }
        
        public void PublishPaymentSuccess(string paymentIntentId, long amount, string currency)
        {
            if (_channel == null || !_channel.IsOpen)
            {
                _logger.LogError("RabbitMQ channel is not open. Cannot publish payment success message.");
                return;
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
                _logger.LogError("RabbitMQ channel is not open. Cannot publish payment failure message.");
                return;
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
            _logger.LogInformation("Disposing RabbitMQ connection and channel.");
            if (_channel != null && _channel.IsOpen)
            {
                _channel.Close();
                _channel.Dispose();
            }
            if (_connection != null && _connection.IsOpen)
            {
                _connection.Close();
                _connection.Dispose();
            }
            GC.SuppressFinalize(this);
        }
    }   
}