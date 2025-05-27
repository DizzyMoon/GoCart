using Product.ProductModels;
using sync_service.Messaging.Events;

namespace sync_service.ProductServices;

public interface IProductService
{
    Task<ProductModel?> createProduct(ProductModel product);
    Task<ProductModel?> updateProduct(ProductModel product);
    Task<ProductModel?> deleteProduct(string productCode);
    Task<ProductModel?> ProcessFailedAddProductEventAsync(AddProductFailedEvent addProductEvent);
}