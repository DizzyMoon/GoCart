using Product.ProductModels;

namespace Product.ProductServices{
    public interface IProductService {
        Task<string> CreateProduct(ProductModel product);
        Task<string> UpdateProduct(string productCode, ProductModel product);
        Task<string> DeleteProduct(string productCode);
        Task<ProductModel> GetProduct(string productCode);
        Task<IEnumerable<ProductModel>> GetQueryCollection();
    }
}
