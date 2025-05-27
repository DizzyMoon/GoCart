using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace sync_service.Messaging.Connection
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
            // TryConnect(); // Defer initial connection to be explicitly called or by a startup service
        }

        public bool IsConnected => _connection != null && _connection.IsOpen && !_disposed;

        public bool TryConnect()
        {
            if (IsConnected) return true;
            lock (_syncRoot)
            {
                if (IsConnected) return true; // Double-check lock

                _logger.LogInformation("Product Service: RabbitMQ Client is trying to connect...");
                for (int retries = 0; retries < _retryCount; retries++)
                {
                    try
                    {
                        _connection = _connectionFactory.CreateConnection();
                        if (IsConnected)
                        {
                            _connection.ConnectionShutdown += OnConnectionShutdown;
                            _connection.CallbackException += OnCallbackException;
                            _connection.ConnectionBlocked += OnConnectionBlocked;
                            _logger.LogInformation($"Product Service: RabbitMQ persistent connection acquired to '{_connection.Endpoint.HostName}' and is open.");
                            // Queues and bindings should be declared after connection.
                            // This can be called here or, more robustly, once at application startup after ensuring connection.
                            // For this example, we'll assume DeclareQueuesAndBindings is called explicitly at startup.
                            return true;
                        }
                    }
                    catch (BrokerUnreachableException ex)
                    {
                        _logger.LogWarning(ex, $"Product Service: RabbitMQ connection failed on attempt {retries + 1}/{_retryCount}. Retrying in 5s...");
                        Thread.Sleep(5000);
                    }
                    catch (Exception ex)
                    {
                         _logger.LogWarning(ex, $"Product Service: An unexpected error occurred trying to connect to RabbitMQ on attempt {retries + 1}/{_retryCount}. Retrying in 5s...");
                        Thread.Sleep(5000);
                    }
                }
                _logger.LogError($"Product Service: Could not connect to RabbitMQ after {_retryCount} attempts.");
                return false;
            }
        }

        public IModel CreateChannel()
        {
            if (!IsConnected)
            {
                _logger.LogWarning("Product Service: RabbitMQ connection is not open. Attempting to connect before creating channel.");
                if (!TryConnect())
                {
                     throw new InvalidOperationException("RabbitMQ connection is not open and could not be established.");
                }
            }
            return _connection.CreateModel();
        }

        public void DeclareQueuesAndBindings()
        {
            if (!IsConnected)
            {
                _logger.LogWarning("Product Service: Cannot declare queues/bindings, RabbitMQ connection is not open.");
                if(!TryConnect()) // Attempt to reconnect if not connected
                {
                     _logger.LogError("Product Service: Failed to connect to RabbitMQ. Queues and bindings will not be declared.");
                     return; // Exit if connection cannot be established
                }
            }

            try
            {
                using (var channel = _connection.CreateModel()) // Use a temporary channel for declarations
                {
                    _logger.LogInformation("Product Service: Declaring RabbitMQ exchanges, queues, and bindings...");

                    // Exchange where Product Service publishes events (Prouduct Service needs to know about it)
                    // This declaration is idempotent.
                    channel.ExchangeDeclare(
                        exchange: "add_product_events_exchange", 
                        type: ExchangeType.Direct, 
                        durable: true, 
                        autoDelete: false);
                    _logger.LogInformation("Exchange 'add_product_events_exchange' declared/ensured by Product Service.");

                    // Dead Letter Exchange for Product Service (for messages Product Service fails to process)
                    string addProductDlExchangeName = "add_product_dead_letter_exchange";
                    channel.ExchangeDeclare(exchange: addProductDlExchangeName, type: ExchangeType.Fanout, durable: true);
                    _logger.LogInformation("Dead Letter Exchange '{DLExchangeName}' declared for Product Service.", addProductDlExchangeName);
                    
                    string addProductDlQueueName = "add_product_dead_letter_queue";
                    channel.QueueDeclare(queue: addProductDlQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
                    channel.QueueBind(queue: addProductDlQueueName, exchange: addProductDlExchangeName, routingKey: ""); // Fanout ignores routing key
                    _logger.LogInformation("Dead Letter Queue '{DLQueueName}' declared and bound to '{DLExchangeName}'.", addProductDlQueueName, addProductDlExchangeName);

                    var deadLetterArgs = new Dictionary<string, object>
                    {
                        { "x-dead-letter-exchange", addProductDlExchangeName }
                        // { "x-dead-letter-routing-key", "your_dl_routing_key_if_dlx_is_not_fanout" } // Optional
                    };

                    // Queue for AddProductSucceededEvent
                    string addProductSuccessQueueName = "add_product_succeeded_event_queue";
                    channel.QueueDeclare(queue: addProductSuccessQueueName, durable: true, exclusive: false, autoDelete: false, arguments: deadLetterArgs);
                    channel.QueueBind(queue: addProductSuccessQueueName, exchange: "add_product_events_exchange", routingKey: "add_product.succeeded");
                    _logger.LogInformation("Queue '{QueueName}' declared, bound to 'add_product_events_exchange' with key 'add_product.succeeded', and configured with DLX.", addProductSuccessQueueName);
                    
                    // Queue for AddProductFailedEvent
                    string addProductFailedQueueName = "add_product_failed_event_queue";
                    channel.QueueDeclare(queue: addProductFailedQueueName, durable: true, exclusive: false, autoDelete: false, arguments: deadLetterArgs);
                    channel.QueueBind(queue: addProductFailedQueueName, exchange: "add_product_events_exchange", routingKey: "add_product.failed");
                    _logger.LogInformation("Queue '{QueueName}' declared, bound to 'add_product_events_exchange' with key 'add_product.failed', and configured with DLX.", addProductFailedQueueName);

                    _logger.LogInformation("Product Service: RabbitMQ infrastructure (queues, bindings, DLX) declaration complete.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Product Service: An error occurred while declaring RabbitMQ infrastructure.");
                // Depending on the severity, you might want to rethrow or handle this to prevent app startup.
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
                _logger.LogInformation("Product Service: RabbitMQ connection disposed.");
            }
            catch (IOException ex)
            {
                _logger.LogCritical(ex, "Product Service: Cannot dispose RabbitMQ connection.");
            }
        }

        private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e) => _logger.LogWarning("Product Service: RabbitMQ connection is blocked. Reason: {Reason}", e.Reason);
        private void OnCallbackException(object sender, CallbackExceptionEventArgs e) => _logger.LogWarning(e.Exception, "Product Service: A callback exception occurred. Detail: {Detail}", e.Detail);
        private void OnConnectionShutdown(object sender, ShutdownEventArgs reason) => _logger.LogWarning("Product Service: RabbitMQ connection is on shutdown. Reason: {ReplyText}", reason.ReplyText);
    }
}
