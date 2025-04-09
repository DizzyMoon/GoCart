using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

using Product.ProductModels;
using Product.ProductServices;

namespace Product.ProductControllers{
    [ApiController]
    [Route("[controller]")]
    class ProductController : ControllerBase {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpPost]
        public async Task<ActionResult<string>> CreateProduct([FromBody] ProductModel product)
        {
            if (product == null)
            {
                return BadRequest("Product data is required.");
            }

            try
            {
                // Call the ProductService to create the product
                var productCode = await _productService.CreateProduct(product);

                // Return the ProductCode of the created product
                return CreatedAtAction(nameof(GetProduct), new { productCode }, productCode);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductModel))]
        [Produces("application/json")]
        [HttpGet]
        [Route("")]
        public async Task<IActionResult> GetQueryCollection() {
            var result = await _productService.GetQueryCollection();
            return Ok(result);
        }



        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductModel))]
        [Produces("application/json")]
        [HttpGet]
        [Route("{productCode}")]
        public async Task<ActionResult<ProductModel>> GetProduct(string productCode)
        {
            if (productCode == null || productCode == ""){
                return BadRequest("A valid product code must be provided.");
            }

            try {
                var result = await _productService.GetProduct(productCode);
                return Ok(result);
            } catch (Exception e){
                return StatusCode(500, $"Internal server error: {e.Message}");
            }
        }

    }
}

