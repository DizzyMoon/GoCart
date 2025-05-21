using Product.ProductModels;

namespace DefaultNamespace;
using Npgsql;
using Elastic.Clients.Elasticsearch;
using Npgsql;
using Nest;

public class SyncService(ElasticClient esClient)
{
    public async Task SyncElasticsearchWithPostgres()
    {
        
        // Delete and recreate index (optionally add mappings here)
        if ((await esClient.Indices.ExistsAsync("products")).Exists)
            await esClient.Indices.DeleteAsync("products");

        await esClient.Indices.CreateAsync("products");

        var products = new List<ProductModel>();

        await using var conn = new NpgsqlConnection("Host=localhost;Username=postgres;Password=yourpassword;Database=yourdb");
        await conn.OpenAsync();

        await using var cmd = new NpgsqlCommand("SELECT id, name, price FROM products", conn);
        await using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            products.Add(new ProductModel
            {
                ProductCode = reader.GetString(0),
                Name = reader.GetString(1),
                Price = reader.GetDouble(2),
                Description = reader.GetString(3),
                Variants = reader.GetFieldValue<string[]>(4),
                Discounts = reader.GetDouble(5),
                Images = reader.GetFieldValue<string[]>(6),
                Specifications = reader.GetFieldValue<Dictionary<string, object>>(7)
            });
        }

        var bulkResponse = await esClient.BulkAsync(b => b
            .Index("products")
            .IndexMany(products)
        );

        if (bulkResponse.Errors)
        {
            Console.WriteLine("Error during bulk insert:");
            foreach (var item in bulkResponse.ItemsWithErrors)
                Console.WriteLine($" - {item.Error.Reason}");
        }
        else
        {
            Console.WriteLine($"Synced {products.Count} products to Elasticsearch.");
        }
    }

}