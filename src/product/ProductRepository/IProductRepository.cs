using System.Collections.Generic;
using System.Threading.Tasks;
using Product.ProductModels;

namespace Product.ProductRepository {
  public interface IProductRepository{
    Task<IEnumerable<ProductModel>> GetQueryCollection();
    Task<ProductModel?> Get(string productCode);
    Task<ProductModel?> Create(ProductModel product);
    //Task<ProductModel?> Delete(int productId);
  }
}