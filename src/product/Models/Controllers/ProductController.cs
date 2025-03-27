using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;


[Route("api/[controller]")]
class ProductController : ControllerBase {
    private readonly IProductService _productService;

    public ProductController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpPost]
    public async Task<ActionResult<string>> CreateProduct([FromBody] Product product)
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

    [HttpGet("{productCode}")]
    public async Task<ActionResult<Product>> GetProduct(string productCode)
    {
        if (productCode == null || productCode == ""){
            return BadRequest("A valid product code must be provided.");
        }

        try {
            return Ok(await _productService.GetProduct(productCode));
        } catch (Exception e){
            return StatusCode(500, $"Internal server error: {e.Message}");
        }
    }

}