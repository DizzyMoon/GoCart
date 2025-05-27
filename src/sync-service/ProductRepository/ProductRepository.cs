using Nest;
using sync_service.ProductModels;

namespace sync_service.ProductRepository
{
    public class ProductRepository : IProductRepository
    {
        private readonly ElasticClient _esClient;
        private readonly ILogger<ProductRepository> _logger;
        private const string ProductsIndexName = "dls-exam1";

        public ProductRepository(ElasticClient esClient, ILogger<ProductRepository> logger)
        {
            _esClient = esClient;
            _logger = logger;
        }

        public async Task<ProductModel> Create(ProductModel product)
        {
            if (product == null)
            {
                _logger.LogError("Attempted to create a null product.");
                throw new ArgumentException(nameof(product));
            }

            if (string.IsNullOrEmpty(product.ProductCode))
            {
                _logger.LogError("ProductCode is missing. Cannot create product in Elasticsearch without an ID.");
                throw new ArgumentException(
                    "Product.ProductCode cannot be null or empty when creating a document with a specific ID.",
                    nameof(product.ProductCode));
            }

            _logger.LogInformation(
                "Attempting to create product with ProductCode: {ProductCode} in Elasticsearch index: {IndexName}",
                product.ProductCode, ProductsIndexName);

            try
            {
                
                // vaesClient.IndexAsync(product, idx => idx.Index("products").Id(product.ProductCode));
                var response =
                    await _esClient.CreateAsync(product, c => c.Index(ProductsIndexName).Id(product.ProductCode));

                if (response.IsValid && response.Result == Result.Created)
                {
                    _logger.LogInformation(
                        "Product with ProductCode: {ProductCode} created successfully in Elasticsearch. Version: {Version}",
                        response.Id, response.Version);
                    return product;
                }

                var errorMessage =
                    $"Failed to create product with ProductCode: {product.ProductCode} in Elasticsearch. IsValid: {response.IsValid}, Result: {response.Result}.";
                if (response.ServerError != null)
                {
                    errorMessage +=
                        $" ServerError: {response.ServerError.Error?.Reason} (Status: {response.ServerError.Status})";
                }

                if (!string.IsNullOrEmpty(response.DebugInformation))
                {
                    errorMessage += $"\nDebugInformation:\n{response.DebugInformation}";
                }

                if (response.OriginalException != null)
                {
                    errorMessage += $"\nOriginalException: {response.OriginalException.Message}";
                }

                _logger.LogError(response.OriginalException, errorMessage);
                throw new Exception(errorMessage, response.OriginalException);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "An unexpected error occurred while creating product with ProductCode: {ProductCode} in Elasticsearch.",
                    product.ProductCode);
                throw;
            }
        }
    }
}