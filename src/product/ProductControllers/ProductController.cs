using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

using Product.ProductModels;
using Product.ProductServices;

namespace Product.ProductControllers {
    [ApiController]
    [Route("[controller]")]
    public class ProductController : ControllerBase {
        private readonly IProductService _productService;

        public ProductController(IProductService productService)
        {
            _productService = productService;
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
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [Produces("application/json")]
        [HttpGet]
        [Route("{productCode}")]
        public async Task<ActionResult<ProductModel>> Get(string productCode)
        {
            if (string.IsNullOrEmpty(productCode))
            {
                return BadRequest("A valid product code must be provided.");
            }

            try {
                var result = await _productService.Get(productCode);
                if (result == null)
                {
                    return NotFound();
                }
                return Ok(result);
            } catch (Exception e){
                return StatusCode(500, $"Internal server error: {e.Message}");
            }
        }

        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProductModel))]
        [ProducesResponseType(StatusCodes.Status405MethodNotAllowed, Type = typeof(ProductModel))]
        [Produces("application/json")]
        [HttpPost]
        [Route("")]
        public async Task<ActionResult> Create([FromBody] CreateProductModel product)
        {
            try
            {
                var result = await _productService.Create(product);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(StatusCodes.Status405MethodNotAllowed, ex.Message);
            }
        }

    }
}

