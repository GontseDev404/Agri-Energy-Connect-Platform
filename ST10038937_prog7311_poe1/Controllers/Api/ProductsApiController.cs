using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ST10038937_prog7311_poe1.Data;
using ST10038937_prog7311_poe1.Models;
using Microsoft.AspNetCore.Identity;
using ST10038937_prog7311_poe1.Services;
using System.Security.Claims;

namespace ST10038937_prog7311_poe1.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class ProductsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager, 
            IAuditService auditService,
            ILogger<ProductsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _auditService = auditService;
            _logger = logger;
        }

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts(
            [FromQuery] string? search, 
            [FromQuery] string? category,
            [FromQuery] int? farmerId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = _context.Products
                    .Include(p => p.Farmer)
                    .AsNoTracking()
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(p => p.Name.Contains(search) || p.Description.Contains(search));
                }

                // Apply category filter
                if (!string.IsNullOrWhiteSpace(category))
                {
                    query = query.Where(p => p.Category == category);
                }

                // Apply farmer filter
                if (farmerId.HasValue)
                {
                    query = query.Where(p => p.FarmerId == farmerId.Value);
                }

                // Apply role-based filtering
                if (User.IsInRole("Farmer"))
                {
                    var user = await _userManager.GetUserAsync(User);
                    var farmer = await _context.Farmers.FirstOrDefaultAsync(f => f.UserId == user.Id);
                    if (farmer != null)
                    {
                        query = query.Where(p => p.FarmerId == farmer.FarmerId);
                    }
                }

                // Apply pagination
                var totalCount = await query.CountAsync();
                var products = await query
                    .OrderByDescending(p => p.ProductionDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var productDtos = products.Select(p => new ProductDto
                {
                    Id = p.ProductId,
                    Name = p.Name,
                    Description = p.Description,
                    Category = p.Category,
                    Price = p.Price,
                    QuantityAvailable = p.QuantityAvailable,
                    ProductionDate = p.ProductionDate,
                    FarmerName = p.Farmer?.Name ?? "Unknown",
                    FarmerId = p.FarmerId
                });

                Response.Headers.Add("X-Total-Count", totalCount.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());

                return Ok(productDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products");
                return StatusCode(500, new { message = "An error occurred while retrieving products" });
            }
        }

        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetProduct(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Farmer)
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product == null)
                {
                    return NotFound(new { message = "Product not found" });
                }

                // Check if user has access to this product
                if (User.IsInRole("Farmer"))
                {
                    var user = await _userManager.GetUserAsync(User);
                    var farmer = await _context.Farmers.FirstOrDefaultAsync(f => f.UserId == user.Id);
                    if (farmer == null || product.FarmerId != farmer.FarmerId)
                    {
                        return Forbid();
                    }
                }

                var productDto = new ProductDto
                {
                    Id = product.ProductId,
                    Name = product.Name,
                    Description = product.Description,
                    Category = product.Category,
                    Price = product.Price,
                    QuantityAvailable = product.QuantityAvailable,
                    ProductionDate = product.ProductionDate,
                    FarmerName = product.Farmer?.Name ?? "Unknown",
                    FarmerId = product.FarmerId
                };

                return Ok(productDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product {ProductId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the product" });
            }
        }

        // POST: api/Products
        [HttpPost]
        public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductDto createProductDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                // Get farmer profile
                var farmer = await _context.Farmers.FirstOrDefaultAsync(f => f.UserId == user.Id);
                if (farmer == null)
                {
                    return BadRequest(new { message = "Farmer profile not found" });
                }

                var product = new Product
                {
                    Name = createProductDto.Name,
                    Description = createProductDto.Description,
                    Category = createProductDto.Category,
                    Price = createProductDto.Price,
                    QuantityAvailable = createProductDto.QuantityAvailable,
                    ProductionDate = createProductDto.ProductionDate ?? DateTime.UtcNow,
                    FarmerId = farmer.FarmerId
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                await _auditService.LogActionAsync(user.Id, "Product Created", $"Product: {product.Name}, ID: {product.ProductId}");

                var productDto = new ProductDto
                {
                    Id = product.ProductId,
                    Name = product.Name,
                    Description = product.Description,
                    Category = product.Category,
                    Price = product.Price,
                    QuantityAvailable = product.QuantityAvailable,
                    ProductionDate = product.ProductionDate,
                    FarmerName = farmer.Name,
                    FarmerId = product.FarmerId
                };

                return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, productDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return StatusCode(500, new { message = "An error occurred while creating the product" });
            }
        }

        // PUT: api/Products/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductDto updateProductDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound(new { message = "Product not found" });
                }

                // Check if user has access to this product
                if (User.IsInRole("Farmer"))
                {
                    var user = await _userManager.GetUserAsync(User);
                    var farmer = await _context.Farmers.FirstOrDefaultAsync(f => f.UserId == user.Id);
                    if (farmer == null || product.FarmerId != farmer.FarmerId)
                    {
                        return Forbid();
                    }
                }

                // Update product properties
                product.Name = updateProductDto.Name ?? product.Name;
                product.Description = updateProductDto.Description ?? product.Description;
                product.Category = updateProductDto.Category ?? product.Category;
                product.Price = updateProductDto.Price ?? product.Price;
                product.QuantityAvailable = updateProductDto.QuantityAvailable ?? product.QuantityAvailable;
                if (updateProductDto.ProductionDate.HasValue)
                {
                    product.ProductionDate = updateProductDto.ProductionDate.Value;
                }

                await _context.SaveChangesAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                await _auditService.LogActionAsync(currentUser?.Id ?? "unknown", "Product Updated", $"Product: {product.Name}, ID: {product.ProductId}");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product {ProductId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the product" });
            }
        }

        // DELETE: api/Products/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound(new { message = "Product not found" });
                }

                // Check if user has access to this product
                if (User.IsInRole("Farmer"))
                {
                    var user = await _userManager.GetUserAsync(User);
                    var farmer = await _context.Farmers.FirstOrDefaultAsync(f => f.UserId == user.Id);
                    if (farmer == null || product.FarmerId != farmer.FarmerId)
                    {
                        return Forbid();
                    }
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                await _auditService.LogActionAsync(currentUser?.Id ?? "unknown", "Product Deleted", $"Product: {product.Name}, ID: {product.ProductId}");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product {ProductId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the product" });
            }
        }

        // GET: api/Products/categories
        [HttpGet("categories")]
        public async Task<ActionResult<IEnumerable<string>>> GetCategories()
        {
            try
            {
                var categories = await _context.Products
                    .Select(p => p.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return Ok(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories");
                return StatusCode(500, new { message = "An error occurred while retrieving categories" });
            }
        }
    }

    // DTOs for API
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int QuantityAvailable { get; set; }
        public DateTime ProductionDate { get; set; }
        public string FarmerName { get; set; } = string.Empty;
        public int FarmerId { get; set; }
    }

    public class CreateProductDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int QuantityAvailable { get; set; }
        public DateTime? ProductionDate { get; set; }
    }

    public class UpdateProductDto
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Category { get; set; }
        public decimal? Price { get; set; }
        public int? QuantityAvailable { get; set; }
        public DateTime? ProductionDate { get; set; }
    }
} 