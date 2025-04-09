using Npgsql;
using System.Threading.Tasks;
using System;

using Product.ProductRepository;
using Product.ProductModels;

namespace Product.ProductServices{
    class ProductService : IProductService{
        
        private IProductRepository _productRepository;

        public ProductService (IProductRepository productRepository){
            _productRepository = productRepository;
        }
        
        public async Task<string> CreateProduct(ProductModel product)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<ProductModel>> GetQueryCollection() {
            return await _productRepository.GetQueryCollection();
        }

        public Task<string> UpdateProduct(string productCode, ProductModel product)
        {
            throw new NotImplementedException();
        }

        public Task<string> DeleteProduct(string productCode)
        {
            throw new NotImplementedException();
        }

        public async Task<ProductModel> GetProduct(string productCode)
        {
            return await _productRepository.Get(productCode);
        }
    }
}
