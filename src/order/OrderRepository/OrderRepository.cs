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
          Id = reader.GetInt32(reader.GetOrdinal("id")),
          OrderNumber = reader.GetString(reader.GetOrdinal("orderNumber"))
        });
      }

      return orders;
    }

    public async Task<OrderModel> Get(int orderId)
    {
      OrderModel order = null;
      
      await using var connection = await GetConnectionAsync();
      await using var command = new NpgsqlCommand("SELECT * FROM orders WHERE id = @orderId", connection);
      command.Parameters.AddWithValue("orderId", orderId);
      await using var reader = await command.ExecuteReaderAsync();

      if (await reader.ReadAsync())
      {
        order = new OrderModel
        {
          Id = reader.GetInt32(reader.GetOrdinal("id")),
          OrderNumber = reader.GetString(reader.GetOrdinal("orderNumber"))
        };
      }

      return order;
    }
  }
}