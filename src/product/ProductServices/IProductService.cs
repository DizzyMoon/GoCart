using System.Collections.Generic;
using System.Threading.Tasks;
using Product.ProductModels;

namespace Product.ProductServices{
    public interface IProductService {
        Task<IEnumerable<ProductModel>> GetQueryCollection();
        Task<ProductModel?> Get(string productCode);
        Task<ProductModel?> Create(CreateProductModel product);
        Task<bool> Update(ProductModel product);
        Task<bool> Delete(string productCode);
    }
}
