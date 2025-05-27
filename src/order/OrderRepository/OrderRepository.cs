using Npgsql;
using Order.OrderModels;

namespace Order.OrderRepository
{
    public class OrderRepository : IOrderRepository
    {
        private readonly NpgsqlDataSource _dataSource;

        public OrderRepository(NpgsqlDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        private async Task<NpgsqlConnection> GetConnectionAsync()
        {
            return await _dataSource.OpenConnectionAsync();
        }

        public async Task<IEnumerable<OrderModel>> GetQueryCollection()
        {
            var orders = new List<OrderModel>();

            await using var connection = await GetConnectionAsync();
            await using var command = new NpgsqlCommand("SELECT * FROM orders", connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                orders.Add(new OrderModel
                {
                    Id = reader.GetInt32(reader.GetOrdinal("ID")),
                    OrderNumber = reader.GetString(reader.GetOrdinal("OrderNumber")),
                    OrderDate = reader.GetDateTime(reader.GetOrdinal("OrderDate")),
                    PaymentIntentId = reader.GetString(reader.GetOrdinal("PaymentIntentId")),
                    Status = reader.GetString(reader.GetOrdinal("Status"))
                });
            }

            return orders;
        }

        public async Task<OrderModel?> Get(int orderId)
        {
            OrderModel? order = null;

            await using var connection = await GetConnectionAsync();
            await using var command = new NpgsqlCommand("SELECT * FROM orders WHERE ID = @OrderId", connection);
            command.Parameters.AddWithValue("@OrderId", orderId);
            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                order = new OrderModel
                {
                    Id = reader.GetInt32(reader.GetOrdinal("ID")),
                    OrderNumber = reader.GetString(reader.GetOrdinal("OrderNumber")),
                    OrderDate = reader.GetDateTime(reader.GetOrdinal("OrderDate")),
                    PaymentIntentId = reader.GetString(reader.GetOrdinal("PaymentIntentId")),
                    Status = reader.GetString(reader.GetOrdinal("Status"))
                };
            }

            return order;
        }

        public async Task<OrderModel?> GetByPaymentIntentIdAsync(string paymentIntentId)
        {
            OrderModel? order = null;
            await using var connection = await GetConnectionAsync();
            await using var command = new NpgsqlCommand("SELECT * FROM orders WHERE PaymentIntentId = @PaymentIntentId",
                connection);
            command.Parameters.AddWithValue("@PaymentIntentId", paymentIntentId);
            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                order = new OrderModel
                {
                    Id = reader.GetInt32(reader.GetOrdinal("ID")),
                    OrderNumber = reader.GetString(reader.GetOrdinal("OrderNumber")),
                    OrderDate = reader.GetDateTime(reader.GetOrdinal("OrderDate")),
                    PaymentIntentId = reader.GetString(reader.GetOrdinal("PaymentIntentId")),
                    Status = reader.GetString(reader.GetOrdinal("Status"))
                };
            }

            return order;
        }

        public async Task<OrderModel> Create(OrderModel order)
        {
            await using var connection = await GetConnectionAsync();
            await using var command = new NpgsqlCommand(
                "INSERT INTO orders (OrderNumber, OrderDate, PaymentIntentId, Status) VALUES (@OrderNumber, @OrderDate, @PaymentIntentId, @Status) RETURNING ID, OrderNumber, OrderDate, PaymentIntentId, Status;",
                connection);

            command.Parameters.AddWithValue("@OrderNumber", order.OrderNumber);
            command.Parameters.AddWithValue("@OrderDate", order.OrderDate);
            command.Parameters.AddWithValue("@PaymentIntentId", order.PaymentIntentId);
            command.Parameters.AddWithValue("@Status", order.Status);

            OrderModel? newOrder = null;
            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                newOrder = new OrderModel
                {
                    Id = reader.GetInt32(reader.GetOrdinal("ID")),
                    OrderNumber = reader.GetString(reader.GetOrdinal("OrderNumber")),
                    OrderDate = reader.GetDateTime(reader.GetOrdinal("OrderDate")),
                    PaymentIntentId = reader.GetString(reader.GetOrdinal("PaymentIntentId")),
                    Status = reader.GetString(reader.GetOrdinal("Status"))
                };
            }

            if (newOrder == null)
            {
                throw new NpgsqlException("Failed to create order or retrieve the created order details.");
            }

            return newOrder;
        }

        public async Task<OrderModel?> Delete(int orderId)
        {
            var orderModelToDelete = await Get(orderId);

            if (orderModelToDelete == null)
            {
                return null;
            }

            await using var connection = await GetConnectionAsync();
            await using var command = new NpgsqlCommand("DELETE FROM orders WHERE ID = @OrderId", connection);
            command.Parameters.AddWithValue("@OrderId", orderId);

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return orderModelToDelete;
        }
    }
}