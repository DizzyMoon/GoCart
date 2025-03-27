public interface IProductService {
    Task<string> CreateProduct(Product product);
    Task<string> UpdateProduct(string productCode, Product product);
    Task<string> DeleteProduct(string productCode);
    Task<Product> GetProduct(string productCode);
}