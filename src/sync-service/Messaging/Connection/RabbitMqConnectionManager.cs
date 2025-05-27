using product.Messaging.Connection;
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

            var rabbitMQHost = _configuration["RABBITMQ_HOST"] ?? "rabbitmq";
            var rabbitMQUser = _configuration["RABBITMQ_USER"] ?? "guest";
            var rabbitMQPassword = _configuration["RABBITMQ_PASSWORD"] ?? "guest";
            var rabbitMQPort = _configuration.GetValue<int>("RABBITMQ_PORT", 5672);

            _connectionFactory = new ConnectionFactory()
            {
                HostName = rabbitMQHost,
                UserName = rabbitMQUser,
                Password = rabbitMQPassword,
                Port = rabbitMQPort,
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

                _logger.LogInformation("Order Service: RabbitMQ Client is trying to connect...");
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
                            _logger.LogInformation($"Order Service: RabbitMQ persistent connection acquired to '{_connection.Endpoint.HostName}' and is open.");
                            // Queues and bindings should be declared after connection.
                            // This can be called here or, more robustly, once at application startup after ensuring connection.
                            // For this example, we'll assume DeclareQueuesAndBindings is called explicitly at startup.
                            return true;
                        }
                    }
                    catch (BrokerUnreachableException ex)
                    {
                        _logger.LogWarning(ex, $"Order Service: RabbitMQ connection failed on attempt {retries + 1}/{_retryCount}. Retrying in 5s...");
                        Thread.Sleep(5000);
                    }
                    catch (Exception ex)
                    {
                         _logger.LogWarning(ex, $"Order Service: An unexpected error occurred trying to connect to RabbitMQ on attempt {retries + 1}/{_retryCount}. Retrying in 5s...");
                        Thread.Sleep(5000);
                    }
                }
                _logger.LogError($"Order Service: Could not connect to RabbitMQ after {_retryCount} attempts.");
                return false;
            }
        }

        public IModel CreateChannel()
        {
            if (!IsConnected)
            {
                _logger.LogWarning("Order Service: RabbitMQ connection is not open. Attempting to connect before creating channel.");
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
                _logger.LogWarning("Order Service: Cannot declare queues/bindings, RabbitMQ connection is not open.");
                if(!TryConnect()) // Attempt to reconnect if not connected
                {
                     _logger.LogError("Order Service: Failed to connect to RabbitMQ. Queues and bindings will not be declared.");
                     return; // Exit if connection cannot be established
                }
            }

            try
            {
                using (var channel = _connection.CreateModel()) // Use a temporary channel for declarations
                {
                    _logger.LogInformation("Order Service: Declaring RabbitMQ exchanges, queues, and bindings...");

                    // Exchange where Payment Service publishes events (Order Service needs to know about it)
                    // This declaration is idempotent.
                    channel.ExchangeDeclare(
                        exchange: "payment_events_exchange", 
                        type: ExchangeType.Direct, 
                        durable: true, 
                        autoDelete: false);
                    _logger.LogInformation("Exchange 'payment_events_exchange' declared/ensured by Order Service.");

                    // Dead Letter Exchange for Order Service (for messages Order Service fails to process)
                    string orderDlExchangeName = "order_dead_letter_exchange";
                    channel.ExchangeDeclare(exchange: orderDlExchangeName, type: ExchangeType.Fanout, durable: true);
                    _logger.LogInformation("Dead Letter Exchange '{DLExchangeName}' declared for Order Service.", orderDlExchangeName);
                    
                    string orderDlQueueName = "order_dead_letter_queue";
                    channel.QueueDeclare(queue: orderDlQueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
                    channel.QueueBind(queue: orderDlQueueName, exchange: orderDlExchangeName, routingKey: ""); // Fanout ignores routing key
                    _logger.LogInformation("Dead Letter Queue '{DLQueueName}' declared and bound to '{DLExchangeName}'.", orderDlQueueName, orderDlExchangeName);

                    var deadLetterArgs = new Dictionary<string, object>
                    {
                        { "x-dead-letter-exchange", orderDlExchangeName }
                        // { "x-dead-letter-routing-key", "your_dl_routing_key_if_dlx_is_not_fanout" } // Optional
                    };

                    // Queue for PaymentSucceededEvent
                    string paymentSuccessQueueName = "order_payment_succeeded_event_queue";
                    channel.QueueDeclare(queue: paymentSuccessQueueName, durable: true, exclusive: false, autoDelete: false, arguments: deadLetterArgs);
                    channel.QueueBind(queue: paymentSuccessQueueName, exchange: "payment_events_exchange", routingKey: "payment.succeeded");
                    _logger.LogInformation("Queue '{QueueName}' declared, bound to 'payment_events_exchange' with key 'payment.succeeded', and configured with DLX.", paymentSuccessQueueName);
                    
                    // Queue for PaymentFailedEvent
                    string paymentFailedQueueName = "order_payment_failed_event_queue";
                    channel.QueueDeclare(queue: paymentFailedQueueName, durable: true, exclusive: false, autoDelete: false, arguments: deadLetterArgs);
                    channel.QueueBind(queue: paymentFailedQueueName, exchange: "payment_events_exchange", routingKey: "payment.failed");
                    _logger.LogInformation("Queue '{QueueName}' declared, bound to 'payment_events_exchange' with key 'payment.failed', and configured with DLX.", paymentFailedQueueName);

                    _logger.LogInformation("Order Service: RabbitMQ infrastructure (queues, bindings, DLX) declaration complete.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Order Service: An error occurred while declaring RabbitMQ infrastructure.");
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
                _logger.LogInformation("Order Service: RabbitMQ connection disposed.");
            }
            catch (IOException ex)
            {
                _logger.LogCritical(ex, "Order Service: Cannot dispose RabbitMQ connection.");
            }
        }

        private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e) => _logger.LogWarning("Order Service: RabbitMQ connection is blocked. Reason: {Reason}", e.Reason);
        private void OnCallbackException(object sender, CallbackExceptionEventArgs e) => _logger.LogWarning(e.Exception, "Order Service: A callback exception occurred. Detail: {Detail}", e.Detail);
        private void OnConnectionShutdown(object sender, ShutdownEventArgs reason) => _logger.LogWarning("Order Service: RabbitMQ connection is on shutdown. Reason: {ReplyText}", reason.ReplyText);
    }
}
