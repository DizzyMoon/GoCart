using Npgsql;
using System.Threading.Tasks;
using System;

using Product.ProductRepository;
using Product.ProductModels;

namespace Product.ProductServices{
    class ProductService : IProductService{
        
        private IProductRepository _productRepository;

        public ProductService (IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }
        
        public async Task<IEnumerable<ProductModel>> GetQueryCollection() 
        {
            return await _productRepository.GetQueryCollection();
        }
        
        public async Task<ProductModel> Get(string productCode)
        {
            return await _productRepository.Get(productCode);
        }

        private string UniqueProductCode()
        {
            return $"PROD-{Guid.NewGuid().ToString().ToUpper().Substring(0, 8)}";
        }
        
        public async Task<ProductModel?> Create(CreateProductModel productDTO)
        {
            if (productDTO == null)
            {
                throw new ArgumentNullException(nameof(productDTO));
            }

            string productCode = UniqueProductCode();

            var newProduct = new ProductModel
            {
                ProductCode = productCode,
                Name = productDTO.Name,
                Price = productDTO.Price,
                Description = productDTO.Description,
                Variants = productDTO.Variants,
                Discounts = productDTO.Discounts,
                Images = productDTO.Images,
                Specifications = productDTO.Specifications
            };

            return await _productRepository.Create(newProduct);
        }

        public Task<string> Update(string productCode, ProductModel product)
        {
            throw new NotImplementedException();
        }

        public Task<string> Delete(string productCode)
        {
            throw new NotImplementedException();
        }
    }
}
