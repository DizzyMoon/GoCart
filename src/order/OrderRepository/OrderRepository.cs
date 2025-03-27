using Npgsql;
using Order.OrderModels;

namespace order.OrderRepository
{
  public class OrderRepository : IOrderRepository
  {
    private readonly string _connectionString;

    public OrderRepository(string connectionString)
    {
      _connectionString = connectionString;
    }

    public async Task<IEnumerable<OrderModel>> GetQueryCollection()
    {
      var orders = new List<OrderModel>();

      using (var connection = new NpgsqlConnection(_connectionString))
      {
        await connection.OpenAsync();

        await using (var command = new NpgsqlCommand("SELECT * FROM Orders", connection))
        using (var reader = await command.ExecuteReaderAsync())
        {
          while (await reader.ReadAsync())
          {
            int idOrdinal = reader.GetOrdinal("id");
            int orderNumberOrdinal = reader.GetOrdinal("orderNumber");

            orders.Add(new OrderModel()
            {
              Id = reader.GetInt32(idOrdinal),
              OrderNumber = reader.GetString(orderNumberOrdinal)
            });
          }
        }
      }

      return orders;
    }
  }
}