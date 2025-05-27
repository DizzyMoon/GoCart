using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace payment.Messaging.Connection
{
    public class RabbitMqConnectionManager : IRabbitMqConnectionManager
    {
        private readonly IConnectionFactory _connectionFactory;
        private readonly ILogger<RabbitMqConnectionManager> _logger;
        private readonly IConfiguration _configuration;
        private IConnection _connection;
        private bool _disposed;
        private readonly object _syncRoot = new object();
        private readonly int _retryCount;

        public RabbitMqConnectionManager(IConfiguration configuration, ILogger<RabbitMqConnectionManager> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _retryCount = _configuration.GetValue<int>("RabbitMQ:RetryCount", 5);

            var rabbitMqHost = _configuration["RABBITMQ_HOST"] ?? "rabbitmq";
            var rabbitMqUser = _configuration["RABBITMQ_USER"] ?? "guest";
            var rabbitMqPassword = _configuration["RABBITMQ_PASSWORD"] ?? "guest";
            var rabbitMqPort = _configuration.GetValue<int>("RABBITMQ_PORT", 5672);

            _connectionFactory = new ConnectionFactory()
            {
                HostName = rabbitMqHost,
                UserName = rabbitMqUser,
                Password = rabbitMqPassword,
                Port = rabbitMqPort,
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
            };
            // TryConnect(); // Initial connection attempt can be deferred or handled by a startup service
        }

        public bool IsConnected => _connection != null && _connection.IsOpen && !_disposed;

        public bool TryConnect()
        {
            if (IsConnected) return true;

            lock (_syncRoot)
            {
                if (IsConnected) return true;

                _logger.LogInformation("Payment Service: RabbitMQ Client is trying to connect...");
                for (int retries = 0; retries < _retryCount; retries++)
                {
                    try
                    {
                        _connection = _connectionFactory.CreateConnection();
                        if (IsConnected) // Check again after attempting to create connection
                        {
                            _connection.ConnectionShutdown += OnConnectionShutdown;
                            _connection.CallbackException += OnCallbackException;
                            _connection.ConnectionBlocked += OnConnectionBlocked;
                            _logger.LogInformation($"Payment Service: RabbitMQ persistent connection acquired to '{_connection.Endpoint.HostName}' and is open.");
                            DeclarePrimaryExchanges();
                            return true;
                        }
                    }
                    catch (BrokerUnreachableException ex)
                    {
                        _logger.LogWarning(ex, $"Payment Service: RabbitMQ connection failed on attempt {retries + 1}/{_retryCount}. Retrying in 5s...");
                        Thread.Sleep(5000);
                    }
                    catch (Exception ex)
                    {
                         _logger.LogWarning(ex, $"Payment Service: An unexpected error occurred trying to connect to RabbitMQ on attempt {retries + 1}/{_retryCount}. Retrying in 5s...");
                        Thread.Sleep(5000);
                    }
                }
                _logger.LogError($"Payment Service: Could not connect to RabbitMQ after {_retryCount} attempts.");
                return false;
            }
        }

        public IModel CreateChannel()
        {
            if (!IsConnected)
            {
                _logger.LogWarning("Payment Service: RabbitMQ connection is not open. Attempting to connect before creating channel.");
                if (!TryConnect()) // Try to connect if not already connected.
                {
                    throw new InvalidOperationException("RabbitMQ connection is not open and could not be established.");
                }
            }
            return _connection.CreateModel();
        }
        
        public void DeclarePrimaryExchanges()
        {
            if (!IsConnected)
            {
                _logger.LogWarning("Payment Service: Cannot declare exchanges, RabbitMQ connection is not open.");
                return; // Or attempt TryConnect() if critical, but usually called post-connection.
            }

            try
            {
                using (var channel = _connection.CreateModel()) // Create a temporary channel for declarations
                {
                    _logger.LogInformation("Payment Service: Declaring 'payment_events_exchange' (Direct, Durable).");
                    channel.ExchangeDeclare(
                        exchange: "payment_events_exchange", 
                        type: ExchangeType.Direct, 
                        durable: true, 
                        autoDelete: false, 
                        arguments: null);

                    _logger.LogInformation("Payment Service: Declaring 'saga_compensation_exchange' (Direct, Durable).");
                    channel.ExchangeDeclare(
                        exchange: "saga_compensation_exchange", 
                        type: ExchangeType.Direct, 
                        durable: true, 
                        autoDelete: false, 
                        arguments: null);
                    
                    _logger.LogInformation("Payment Service: Primary exchanges declared.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment Service: An error occurred while declaring primary exchanges.");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            try
            {
                _connection?.Close();
                _connection?.Dispose();
                _logger.LogInformation("Payment Service: RabbitMQ connection disposed.");
            }
            catch (IOException ex)
            {
                _logger.LogCritical(ex, "Payment Service: Cannot dispose RabbitMQ connection.");
            }
        }

        private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e) => _logger.LogWarning("Payment Service: RabbitMQ connection is blocked. Reason: {Reason}", e.Reason);
        private void OnCallbackException(object sender, CallbackExceptionEventArgs e) => _logger.LogWarning(e.Exception, "Payment Service: A callback exception occurred. Detail: {Detail}", e.Detail);
        private void OnConnectionShutdown(object sender, ShutdownEventArgs reason) => _logger.LogWarning("Payment Service: RabbitMQ connection is on shutdown. Reason: {ReplyText}", reason.ReplyText);
    }
}