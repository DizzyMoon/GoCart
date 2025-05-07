using System.Collections.Generic;
using System.Threading.Tasks;
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
          OrderDate = reader.GetDateTime(reader.GetOrdinal("OrderDate"))
        });
      }

      return orders;
    }

    public async Task<OrderModel?> Get(int orderId)
    {
      OrderModel order = null!;
      
      await using var connection = await GetConnectionAsync();
      await using var command = new NpgsqlCommand("SELECT * FROM orders WHERE ID = @orderId", connection);
      command.Parameters.AddWithValue("orderId", orderId);
      await using var reader = await command.ExecuteReaderAsync();

      if (await reader.ReadAsync())
      {
        order = new OrderModel
        {
          Id = reader.GetInt32(reader.GetOrdinal("ID")),
          OrderNumber = reader.GetString(reader.GetOrdinal("OrderNumber")),
          OrderDate = reader.GetDateTime(reader.GetOrdinal("OrderDate"))
        };
      }

      return order;
    }

    public async Task<OrderModel> Create(OrderModel order)
    {
      await using var connection = await GetConnectionAsync();
      await using var command = new NpgsqlCommand(
        "INSERT INTO orders (OrderNumber, OrderDate) VALUES (@OrderNumber, @OrderDate) RETURNING ID, OrderNumber, OrderDate;",
        connection);
      
      command.Parameters.AddWithValue("@OrderNumber", order.OrderNumber);
      command.Parameters.AddWithValue("@OrderDate", order.OrderDate);

      OrderModel newOrder = null!;
      await using var reader = await command.ExecuteReaderAsync();

      if (await reader.ReadAsync())
      {
        newOrder = new OrderModel
        {
          Id = reader.GetInt32(reader.GetOrdinal("ID")),
          OrderNumber = reader.GetString(reader.GetOrdinal("OrderNumber")),
          OrderDate = reader.GetDateTime(reader.GetOrdinal("OrderDate"))
        };
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
      command.Parameters.AddWithValue("OrderId", orderId);

      await command.ExecuteNonQueryAsync();
      return orderModelToDelete;
    }
  }
}