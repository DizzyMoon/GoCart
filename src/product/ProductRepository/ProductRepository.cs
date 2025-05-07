using System;
using System.Collections.Generic;
using Npgsql;
using Product.ProductModels;
using System.Text.Json;
using System.Threading.Tasks;

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

            if (!await reader.ReadAsync())
            {
                return product;
            }
            
            var specificationsJson = reader.GetString(reader.GetOrdinal("specifications"));

            product = new ProductModel {
                ProductCode = reader.GetString(reader.GetOrdinal("productCode")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Price = reader.GetDouble(reader.GetOrdinal("price")),
                Description = reader.GetString(reader.GetOrdinal("description")),
                Variants = reader.GetFieldValue<string[]>(reader.GetOrdinal("variants")),
                Discounts = reader.GetDouble(reader.GetOrdinal("discounts")),
                Images = reader.GetFieldValue<string[]>(reader.GetOrdinal("images")),
                Specifications = JsonSerializer.Deserialize<Dictionary<string, object>>(specificationsJson) ?? new Dictionary<string, object>()
            };

            return product;
        }

        public async Task<bool> Update(ProductModel product)
        {
            if (product == null)
            {
                throw new ArgumentNullException();
            }

            await using var connection = await GetConnectionAsync();
            await using var command = new NpgsqlCommand(@"
                UPDATE products
                    SET Name = @Name,
                        Price = @Price,
                        Description = @Description,
                        Variants = @Variants,
                        Discounts = @Discounts,
                        Images = @Images,
                    Specifications = @Specifications::jsonb
                WHERE  productCode = @ProductCode", connection);
            command.Parameters.AddWithValue("ProductCode", product.ProductCode);
            command.Parameters.AddWithValue("Name", product.Name);
            command.Parameters.AddWithValue("Price", product.Price);
            command.Parameters.AddWithValue("Description", product.Description);
            command.Parameters.AddWithValue("Variants", product.Variants ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("Discounts", product.Discounts ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("Images", product.Images ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("Specifications", JsonSerializer.Serialize(product.Specifications));

            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<bool> Delete(string productCode)
        {
            Console.WriteLine("productCode: " + productCode);
            await using var connection = await GetConnectionAsync();
            await using var command = new NpgsqlCommand(
                @"DELETE FROM products WHERE productCode = @ProductCode", connection);
            
            command.Parameters.AddWithValue("ProductCode", productCode);
            
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }

        public async Task<ProductModel?> Create(ProductModel product)
        {
            if (product == null)
            {
                throw new ArgumentNullException(nameof(product));
            }
            
            await using var connection = await GetConnectionAsync();
            await using var command = new NpgsqlCommand(@"
                INSERT INTO products (ProductCode, Name, Price, Description, Variants, Discounts, Images, Specifications)
                VALUES (@ProductCode, @Name, @Price, @Description, @Variants, @Discounts, @Images, @Specifications::jsonb)
                RETURNING ProductCode, Name, Price, Description, Variants, Discounts, Images, Specifications::TEXT", connection);

            command.Parameters.AddWithValue("ProductCode", product.ProductCode);
            command.Parameters.AddWithValue("Name", product.Name);
            command.Parameters.AddWithValue("Price", product.Price);
            command.Parameters.AddWithValue("Description", product.Description);
            command.Parameters.AddWithValue("Variants", product.Variants ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("Discounts", product.Discounts ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("Images", product.Images);
            command.Parameters.AddWithValue("Specifications", JsonSerializer.Serialize(product.Specifications));

            ProductModel? newProduct = null;
            await using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                string SpecificationsJson = reader.GetString(reader.GetOrdinal("specifications"));
                newProduct = new ProductModel
                {
                    ProductCode = reader.GetString(reader.GetOrdinal("ProductCode")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Price = reader.GetDouble(reader.GetOrdinal("Price")),
                    Description = reader.GetString(reader.GetOrdinal("Description")),
                    Variants = reader.IsDBNull(reader.GetOrdinal("Variants"))
                        ? null
                        : reader.GetFieldValue<string[]>(reader.GetOrdinal("Variants")),
                    Discounts = reader.IsDBNull(reader.GetOrdinal("Discounts"))
                        ? (double?)null
                        : reader.GetDouble(reader.GetOrdinal("Discounts")),
                    Images = reader.GetFieldValue<string[]>(reader.GetOrdinal("Images")),
                    Specifications = JsonSerializer.Deserialize<Dictionary<string, object>>(SpecificationsJson)
                };
            }

            return newProduct;
        }
    }
}
