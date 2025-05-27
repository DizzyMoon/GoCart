using sync_service.ProductModels;
using sync_service.Messaging.Events;
using sync_service.ProductRepository;
using sync_service.Messaging.Consumers;
using Nest;

namespace sync_service.ProductServices;

public class ProductService(
    IProductRepository productRepository,
    ILogger<ProductService> logger) : IProductService
{
    private string GenerateProductCode()
    {
        return Guid.NewGuid().ToString("D").ToUpper();
    }
    
    public async Task<ProductModel?> updateProduct(ProductModel product)
    {
        throw new NotImplementedException();
    }

    public async Task<ProductModel?> deleteProduct(string id)
    {
        throw new NotImplementedException();
    }
    public async Task<ProductModel?> ProcessSuccessfulAddProductEventAsync(AddProductSucceededEvent addProductEvent)
    {
        if (addProductEvent == null)
        {
            throw new ArgumentNullException(nameof(addProductEvent));
        }

        var newProduct = new ProductModel
        {
            ProductCode = GenerateProductCode(),
            Name = addProductEvent.Name,
            Price = addProductEvent.Price,
            Description = addProductEvent.Description,
            Variants = addProductEvent.Variants,
            Discounts = addProductEvent.Discounts,
            Images = addProductEvent.Images,
            Specifications = addProductEvent.Specifications
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

    public async Task<ProductModel?> ProcessFailedAddProductEventAsync(AddProductFailedEvent addProductEvent)
    {
        throw new NotImplementedException();
    }
}