using Npgsql;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using product.Messaging.Events;
using Product.ProductRepository;
using Product.ProductModels;
using product.Messaging.Publishers;

namespace Product.ProductServices{
    class ProductService : IProductService{
        
        private IProductRepository _productRepository;
        private IMessagePublisher _messagePublisher;

        public ProductService (IProductRepository productRepository, IMessagePublisher messagePublisher)
        {
            _productRepository = productRepository;
            _messagePublisher = messagePublisher;
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

            var succeededEvent = new AddProductSucceededEvent
            {
                Name = newProduct.ProductCode,
                Price = newProduct.Price,
                Description = newProduct.Description,
                Variants = newProduct.Variants,
                Discounts = newProduct.Discounts,
                Images = newProduct.Images,
                Specifications = newProduct.Specifications
            };
            

            try
            {
                _messagePublisher.AddProductSucceededEventAsync(succeededEvent);
            }
            catch (Exception e)
            {
                var failedEvent = new AddProductFailedEvent
                {
                    Name = newProduct.Name,
                    Reason = e.Message
                };
                _messagePublisher.AddProductFailedEventAsync(failedEvent);
            }
            
            var response = await _productRepository.Create(newProduct);

            return response;
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
