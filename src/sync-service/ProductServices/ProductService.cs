using Product.ProductModels;
using sync_service.Messaging.Events;
using sync_service.ProductRepository;

namespace sync_service.ProductServices;
using Npgsql;
using Elastic.Clients.Elasticsearch;
using Npgsql;
using Nest;

public class ProductService(ElasticClient esClient, IProductRepository productRepository, ILogger<ProductService> logger) : IProductService
{
    // public async Task SyncElasticsearchWithPostgres()
    // {

    //     // Delete and recreate index (optionally add mappings here)
    //     if ((await esClient.Indices.ExistsAsync("products")).Exists)
    //         await esClient.Indices.DeleteAsync("products");

    //     await esClient.Indices.CreateAsync("products");

    //     var products = new List<ProductModel>();

    //     await using var conn = new NpgsqlConnection("Host=localhost;Username=postgres;Password=yourpassword;Database=yourdb");
    //     await conn.OpenAsync();

    //     await using var cmd = new NpgsqlCommand("SELECT id, name, price FROM products", conn);
    //     await using var reader = await cmd.ExecuteReaderAsync();

    //     while (await reader.ReadAsync())
    //     {
    //         products.Add(new ProductModel
    //         {
    //             ProductCode = reader.GetString(0),
    //             Name = reader.GetString(1),
    //             Price = reader.GetDouble(2),
    //             Description = reader.GetString(3),
    //             Variants = reader.GetFieldValue<string[]>(4),
    //             Discounts = reader.GetDouble(5),
    //             Images = reader.GetFieldValue<string[]>(6),
    //             Specifications = reader.GetFieldValue<Dictionary<string, object>>(7)
    //         });
    //     }

    //     var bulkResponse = await esClient.BulkAsync(b => b
    //         .Index("products")
    //         .IndexMany(products)
    //     );

    //     if (bulkResponse.Errors)
    //     {
    //         Console.WriteLine("Error during bulk insert:");
    //         foreach (var item in bulkResponse.ItemsWithErrors)
    //             Console.WriteLine($" - {item.Error.Reason}");
    //     }
    //     else
    //     {
    //         Console.WriteLine($"Synced {products.Count} products to Elasticsearch.");
    //     }
    // }
    
    private string GenerateProductCode()
    {
        return Guid.NewGuid().ToString("D").ToUpper();
    }

    public async Task<ProductModel?> createProduct(ProductModel product)
    {
        if (product == null)
        {
            throw new ArgumentNullException(nameof(product));
        }

        var newProduct = new ProductModel
        {
            ProductCode = GenerateProductCode(),
            Name = product.Name,
            Price = product.Price,
            Description = product.Description,
            Variants = product.Variants,
            Discounts = product.Discounts,
            Images = product.Images,
            Specifications = product.Specifications
        };

        try
        {
            var createdProduct = await productRepository.Create(newProduct);
            return createdProduct;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"SyncService: Database error creating product from AddProductSucceededEvent");
            throw;
        }
    }

    public async Task<ProductModel?> updateProduct(ProductModel product)
    {
        throw new NotImplementedException();
    }

    public async Task<ProductModel?> deleteProduct(string id)
    {
        throw new NotImplementedException();
    }

    public async Task<ProductModel?> ProcessFailedAddProductEventAsync(AddProductFailedEvent addProductEvent)
    {
        throw new NotImplementedException();
    }

}