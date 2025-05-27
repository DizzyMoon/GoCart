using sync_service.ProductModels;
using sync_service.Messaging.Events;

namespace sync_service.ProductServices;

public interface IProductService
{
    Task<ProductModel?> updateProduct(ProductModel product);
    Task<ProductModel?> deleteProduct(string productCode);
    Task<ProductModel?> ProcessSuccessfulAddProductEventAsync(AddProductSucceededEvent addProductEvent);
    Task<ProductModel?> ProcessFailedAddProductEventAsync(AddProductFailedEvent addProductEvent);
}