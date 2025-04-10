using System.Collections.Generic;
using System.Threading.Tasks;
using Product.ProductModels;

namespace Product.ProductServices{
    public interface IProductService {
        Task<IEnumerable<ProductModel>> GetQueryCollection();
        Task<ProductModel?> Get(string productCode);
        Task<ProductModel?> Create(CreateProductModel product);
        Task<string> Update(string productCode, ProductModel product);
        Task<string> Delete(string productCode);
    }
}
