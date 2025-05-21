using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System;
using System.Threading;

namespace payment.Messaging
{
    public class RabbitMqConnectionManager : IRabbitMqConnectionManager
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<RabbitMqConnectionManager> _logger;
        private IConnection _connection;
        private bool _disposed = false;
        private readonly object _lock = new object();

        public RabbitMqConnectionManager(IConfiguration configuration, ILogger<RabbitMqConnectionManager> logger)
        {
            _configuration = configuration;
            _logger = logger;
            ConnectToRabbitMq();
            DeclareExchangesAndQueues();
        }

        private void ConnectToRabbitMq()
        {
            var rabbitMQUser = _configuration["RABBITMQ_USER"] ?? "guest";
            var rabbitMQPassword = _configuration["RABBITMQ_PASSWORD"] ?? "guest";
            var rabbitMQHost = _configuration["RABBITMQ_HOST"] ?? "rabbitmq";
            var rabbitMQPort = _configuration.GetValue<int>("RABBITMQ_PORT", AmqpTcpEndpoint.DefaultAmqpSslPort);


            var factory = new ConnectionFactory()
            {
                HostName = rabbitMQHost,
                UserName = rabbitMQUser,
                Password = rabbitMQPassword,
                Port = rabbitMQPort,
                DispatchConsumersAsync = true
            };

            int retries = 5;
            int delayMS = 2000;
            for (int i = 0; i < retries; i++)
            {
                try
                {
                    _connection = factory.CreateConnection();
                    _logger.LogInformation("Successfully connected to RabbitMQ.");
                    return;
                }
                catch (BrokerUnreachableException ex)
                {
                    _logger.LogWarning($"Attempt {i + 1}/{retries}: Could not connect to RabbitMQ at {rabbitMQHost}:{rabbitMQPort}. Retrying in {delayMS / 1000} seconds. Error: {ex.Message}");
                    Thread.Sleep(delayMS);
                }
            }

            throw new InvalidOperationException($"Failed to connect to RabbitMQ after {retries} retries.");
        }

        public IModel CreateChannel()
        {
            lock (_lock)
            {
                if (_connection == null || !_connection.IsOpen)
                {
                    _logger.LogWarning("RabbitMQ connection is closed or null. Attempting to reconnect.");
                    ConnectToRabbitMq();
                }
                return _connection.CreateModel();
            }
        }

        public void DeclareExchangesAndQueues()
        {
            using (var channel = CreateChannel())
            {
                channel.ExchangeDeclare(exchange: "payment_exchange", type: ExchangeType.Direct);

                channel.QueueDeclare(queue: "order_payment_success_queue", durable: true, exclusive: false,
                    autoDelete: false, arguments: null);
                channel.QueueDeclare(queue: "order_payment_failure_queue", durable: true, exclusive: false,
                    autoDelete: false, arguments: null);
                channel.QueueDeclare(queue: "payment_notification_queue", durable: true, exclusive: false,
                    autoDelete: false, arguments: null);

                channel.QueueBind(queue: "order_payment_success_queue", exchange: "payment_exchange",
                    routingKey: "payment.success");
                channel.QueueBind(queue: "order_payment_failure_queue", exchange: "payment_exchange",
                    routingKey: "payment.failure");
                channel.QueueBind(queue: "payment_notification_queue", exchange: "payment_exchange",
                    routingKey: "payment.notification");

                _logger.LogInformation("RabbitMQ exchanges and queues declared.");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _logger.LogInformation("Disposing RabbitMQ connection manager.");
                if (_connection != null && _connection.IsOpen)
                {
                    _connection.Close();
                    _connection.Dispose();
                }
            }
            _disposed = true;
        }
    }
}