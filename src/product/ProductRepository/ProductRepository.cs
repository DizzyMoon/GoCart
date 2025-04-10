using Npgsql;
using Product.ProductModels;
using System.Text.Json;

namespace Product.ProductRepository {
    public class ProductRepository : IProductRepository {
        private readonly NpgsqlDataSource _dataSource;

        public ProductRepository(NpgsqlDataSource dataSource) {
            _dataSource = dataSource;
        }

        private async Task<NpgsqlConnection> GetConnectionAsync() {
            return await _dataSource.OpenConnectionAsync();
        }

        public async Task<IEnumerable<ProductModel>> GetQueryCollection() {
          var products = new List<ProductModel>();

          await using var connection = await GetConnectionAsync();
          await using var command = new NpgsqlCommand("SELECT * FROM products", connection);
          await using var reader = await command.ExecuteReaderAsync();


          while (await reader.ReadAsync())
          { 
              string SpecificationsJson = reader.GetString(reader.GetOrdinal("specifications"));

              products.Add(new ProductModel
              {
                ProductCode = reader.GetString(reader.GetOrdinal("productCode")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Price = reader.GetDouble(reader.GetOrdinal("price")),
                Description = reader.GetString(reader.GetOrdinal("description")),
                Variants = reader.GetFieldValue<string[]>(reader.GetOrdinal("variants")),
                Discounts = reader.GetDouble(reader.GetOrdinal("discounts")),
                Images = reader.GetFieldValue<string[]>(reader.GetOrdinal("images")),
                Specifications = JsonSerializer.Deserialize<Dictionary<string, object>>(SpecificationsJson)
              });
          }

          return products;
        }

        public async Task<ProductModel?> Get(string productCode) {
            ProductModel product = null!;

            await using var connection = await GetConnectionAsync();
            await using var command = new NpgsqlCommand("SELECT * FROM products WHERE productCode = @productCode", connection);
            command.Parameters.AddWithValue("productCode", productCode);
            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync()) {
                // Deserialize the specifications field to a dictionary
                string SpecificationsJson = reader.GetString(reader.GetOrdinal("specifications"));

                product = new ProductModel {
                    ProductCode = reader.GetString(reader.GetOrdinal("productCode")),
                    Name = reader.GetString(reader.GetOrdinal("name")),
                    Price = reader.GetDouble(reader.GetOrdinal("price")),
                    Description = reader.GetString(reader.GetOrdinal("description")),
                    Variants = reader.GetFieldValue<string[]>(reader.GetOrdinal("variants")),
                    Discounts = reader.GetDouble(reader.GetOrdinal("discounts")),
                    Images = reader.GetFieldValue<string[]>(reader.GetOrdinal("images")),
                    Specifications = JsonSerializer.Deserialize<Dictionary<string, object>>(SpecificationsJson)
                };
            }

            return product;
        }
    }
}
