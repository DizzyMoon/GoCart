using Npgsql;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
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
        
        public async Task<ProductModel?> Get(string productCode)
        {
            return await _productRepository.Get(productCode);
        }

        private string UniqueProductCode()
        {
            return $"PROD-{Guid.NewGuid().ToString().ToUpper().Substring(0, 8)}";
        }
        
        public async Task<ProductModel?> Create(CreateProductModel productDto)
        {
            ArgumentNullException.ThrowIfNull(productDto);

            var productCode = UniqueProductCode();

            var newProduct = new ProductModel
            {
                ProductCode = productCode,
                Name = productDto.Name,
                Price = productDto.Price,
                Description = productDto.Description,
                Variants = productDto.Variants,
                Discounts = productDto.Discounts,
                Images = productDto.Images,
                Specifications = productDto.Specifications
            };

            return await _productRepository.Create(newProduct);
        }

        public async Task<bool> Update(ProductModel product)
        {
            ArgumentNullException.ThrowIfNull(product);
            return await _productRepository.Update(product);
        }

        public async Task<bool> Delete(string productCode)
        {
            ArgumentNullException.ThrowIfNull(productCode);
            return await _productRepository.Delete(productCode);
        }
    }
}
