using Product.ProductModels;

namespace sync_service.ProductRepository
{
    public interface IProductRepository
    {
        Task<ProductModel> Create(ProductModel product);
    }
}