using Npgsql;
using Order.OrderModels;
using Microsoft.Extensions.Logging;
using System.Data;

namespace order.OrderRepository
{
  public class OrderRepository : IOrderRepository
  {
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<OrderRepository> _logger;

    public OrderRepository(NpgsqlDataSource dataSource, ILogger<OrderRepository> logger)
    {
      _dataSource = dataSource;
      _logger = logger;
      _logger.LogInformation("OrderRepository created with injected NpgsqlDataSource.");
    }

    public async Task<IEnumerable<OrderModel>> GetQueryCollection()
    {
      var orders = new List<OrderModel>();
      _logger.LogInformation("Executing GetQueryCollection using NpgsqlDataSource");

      await using (var connection = await _dataSource.OpenConnectionAsync())
      {
        _logger.LogDebug("Database connection obtained from pool.");
        try
        {
          await using (var command = new NpgsqlCommand("SELECT * FROM orders", connection))
          using (var reader = await command.ExecuteReaderAsync())
          {
            _logger.LogDebug("Command executed, reading results...");
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
            _logger.LogInformation("Retrieved {OrderCount} orders.", orders.Count);
          }
        }
        catch (NpgsqlException pgEx)
        {
          _logger.LogError(pgEx, "!!! PostgreSQL Error during GetQueryCollection. SQL State: {SqlState}", pgEx.SqlState);
          throw;
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "!!! General Error during GetQueryCollection");
          throw;
        }
      }

      return orders;
    }
  }
}