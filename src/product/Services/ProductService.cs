using Npgsql;
using System.Threading.Tasks;
using System;

class ProductService : IProductService{
    private readonly string _connectionString = "Host=localhost;Username=products_user;Password=products;Database=products";
    
    private NpgsqlConnection GetConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }
    
    public async Task<string> CreateProduct(Product product)
    {
        using (var conn = GetConnection())
        {
            await conn.OpenAsync();  // Open the connection asynchronously

            using (var cmd = new NpgsqlCommand("INSERT INTO products (ProductCode, Name, Price, Description, Variants, Discounts, Images, Specifications) VALUES (@ProductCode, @Name, @Price, @Description, @Variants, @Discounts, @Images, @Specifications) RETURNING ProductCode", conn))
            {
                cmd.Parameters.AddWithValue("ProductCode", product.ProductCode);
                cmd.Parameters.AddWithValue("Name", product.Name);
                cmd.Parameters.AddWithValue("Price", product.Price);
                cmd.Parameters.AddWithValue("Description", product.Description);
                cmd.Parameters.AddWithValue("Variants", product.Variants);
                cmd.Parameters.AddWithValue("Discounts", product.Discounts);
                cmd.Parameters.AddWithValue("Images", product.Images);
                cmd.Parameters.AddWithValue("Specifications", product.Specifications);

                // Execute the command asynchronously and get the ProductCode that was inserted
                var result = await cmd.ExecuteScalarAsync();  // Use ExecuteScalarAsync to get a single value
                
                // Return the ProductCode as a string
                return result.ToString();
            }
        }
    }

    public Task<string> UpdateProduct(string productCode, Product product)
    {
        throw new NotImplementedException();
    }

    public Task<string> DeleteProduct(string productCode)
    {
        throw new NotImplementedException();
    }

    public async Task<Product> GetProduct(string productCode)
    {
        using (var conn = GetConnection())
        {
            await conn.OpenAsync();  // Open the connection asynchronously

            using (var cmd = new NpgsqlCommand("SELECT * FROM products WHERE ProductCode = @ProductCode", conn))
            {
                cmd.Parameters.AddWithValue("ProductCode", productCode);

                // Execute the command and retrieve the result
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        // Map the data from the database to a Product object
                        var product = new Product
                        {
                            ProductCode = reader["ProductCode"].ToString(),
                            Name = reader["Name"].ToString(),
                            Price = Convert.ToDouble(reader["Price"]),  // Explicit conversion to double
                            Description = reader["Description"].ToString(),
                            Variants = reader["Variants"] as string[],  // Assuming Variants are stored as a string array
                            Discounts = Convert.ToDouble(reader["Discounts"]),  // Explicit conversion to double
                            Images = reader["Images"] as string[],  // Assuming Images are stored as a string array
                            Specifications = reader["Specifications"] as Dictionary<string, object>  // Assuming Specifications are stored as a dictionary
                        };

                        return product;
                    }
                    else
                    {
                        // Product not found
                        return null;
                    }
                }
            }
        }
    }
}