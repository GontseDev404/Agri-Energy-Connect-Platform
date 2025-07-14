using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ST10038937_prog7311_poe1.Data;
using ST10038937_prog7311_poe1.Models;
using Microsoft.AspNetCore.Identity;
using ST10038937_prog7311_poe1.Services;

namespace ST10038937_prog7311_poe1.Controllers.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin,Employee")]
    public class FarmersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;
        private readonly ILogger<FarmersController> _logger;

        public FarmersController(
            ApplicationDbContext context, 
            UserManager<ApplicationUser> userManager, 
            IAuditService auditService,
            ILogger<FarmersController> logger)
        {
            _context = context;
            _userManager = userManager;
            _auditService = auditService;
            _logger = logger;
        }

        // GET: api/Farmers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FarmerDto>>> GetFarmers(
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                var query = _context.Farmers
                    .Include(f => f.Products)
                    .AsNoTracking()
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(f => f.Name.Contains(search) || 
                                           f.FarmName.Contains(search) || 
                                           f.Email.Contains(search));
                }

                // Apply pagination
                var totalCount = await query.CountAsync();
                var farmers = await query
                    .OrderBy(f => f.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var farmerDtos = farmers.Select(f => new FarmerDto
                {
                    Id = f.FarmerId,
                    Name = f.Name,
                    FarmName = f.FarmName,
                    Email = f.Email,
                    PhoneNumber = f.PhoneNumber,
                    Address = f.Address,
                    ProductCount = f.Products?.Count ?? 0,
                    UserId = f.UserId
                });

                Response.Headers.Add("X-Total-Count", totalCount.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());

                return Ok(farmerDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving farmers");
                return StatusCode(500, new { message = "An error occurred while retrieving farmers" });
            }
        }

        // GET: api/Farmers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<FarmerDetailDto>> GetFarmer(int id)
        {
            try
            {
                var farmer = await _context.Farmers
                    .Include(f => f.Products)
                    .FirstOrDefaultAsync(f => f.FarmerId == id);

                if (farmer == null)
                {
                    return NotFound(new { message = "Farmer not found" });
                }

                var farmerDetailDto = new FarmerDetailDto
                {
                    Id = farmer.FarmerId,
                    Name = farmer.Name,
                    FarmName = farmer.FarmName,
                    Email = farmer.Email,
                    PhoneNumber = farmer.PhoneNumber,
                    Address = farmer.Address,
                    UserId = farmer.UserId,
                    Products = farmer.Products?.Select(p => new ProductSummaryDto
                    {
                        Id = p.ProductId,
                        Name = p.Name,
                        Category = p.Category,
                        Price = p.Price,
                        QuantityAvailable = p.QuantityAvailable,
                        ProductionDate = p.ProductionDate
                    }).ToList() ?? new List<ProductSummaryDto>()
                };

                return Ok(farmerDetailDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving farmer {FarmerId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the farmer" });
            }
        }

        // POST: api/Farmers
        [HttpPost]
        public async Task<ActionResult<FarmerDto>> CreateFarmer([FromBody] CreateFarmerDto createFarmerDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Check if user exists
                var user = await _userManager.FindByEmailAsync(createFarmerDto.Email);
                if (user == null)
                {
                    return BadRequest(new { message = "User account not found for this email" });
                }

                // Check if farmer already exists for this user
                var existingFarmer = await _context.Farmers.FirstOrDefaultAsync(f => f.UserId == user.Id);
                if (existingFarmer != null)
                {
                    return BadRequest(new { message = "Farmer profile already exists for this user" });
                }

                var farmer = new Farmer
                {
                    Name = createFarmerDto.Name,
                    FarmName = createFarmerDto.FarmName,
                    Email = createFarmerDto.Email,
                    PhoneNumber = createFarmerDto.PhoneNumber,
                    Address = createFarmerDto.Address,
                    UserId = user.Id
                };

                _context.Farmers.Add(farmer);
                await _context.SaveChangesAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                await _auditService.LogActionAsync(currentUser?.Id ?? "unknown", "Farmer Created", $"Farmer: {farmer.Name}, ID: {farmer.FarmerId}");

                var farmerDto = new FarmerDto
                {
                    Id = farmer.FarmerId,
                    Name = farmer.Name,
                    FarmName = farmer.FarmName,
                    Email = farmer.Email,
                    PhoneNumber = farmer.PhoneNumber,
                    Address = farmer.Address,
                    ProductCount = 0,
                    UserId = farmer.UserId
                };

                return CreatedAtAction(nameof(GetFarmer), new { id = farmer.FarmerId }, farmerDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating farmer");
                return StatusCode(500, new { message = "An error occurred while creating the farmer" });
            }
        }

        // PUT: api/Farmers/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFarmer(int id, [FromBody] UpdateFarmerDto updateFarmerDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var farmer = await _context.Farmers.FindAsync(id);
                if (farmer == null)
                {
                    return NotFound(new { message = "Farmer not found" });
                }

                // Update farmer properties
                farmer.Name = updateFarmerDto.Name ?? farmer.Name;
                farmer.FarmName = updateFarmerDto.FarmName ?? farmer.FarmName;
                farmer.PhoneNumber = updateFarmerDto.PhoneNumber ?? farmer.PhoneNumber;
                farmer.Address = updateFarmerDto.Address ?? farmer.Address;

                await _context.SaveChangesAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                await _auditService.LogActionAsync(currentUser?.Id ?? "unknown", "Farmer Updated", $"Farmer: {farmer.Name}, ID: {farmer.FarmerId}");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating farmer {FarmerId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the farmer" });
            }
        }

        // DELETE: api/Farmers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFarmer(int id)
        {
            try
            {
                var farmer = await _context.Farmers
                    .Include(f => f.Products)
                    .FirstOrDefaultAsync(f => f.FarmerId == id);

                if (farmer == null)
                {
                    return NotFound(new { message = "Farmer not found" });
                }

                // Check if farmer has products
                if (farmer.Products?.Any() == true)
                {
                    return BadRequest(new { message = "Cannot delete farmer with existing products" });
                }

                _context.Farmers.Remove(farmer);
                await _context.SaveChangesAsync();

                var currentUser = await _userManager.GetUserAsync(User);
                await _auditService.LogActionAsync(currentUser?.Id ?? "unknown", "Farmer Deleted", $"Farmer: {farmer.Name}, ID: {farmer.FarmerId}");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting farmer {FarmerId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the farmer" });
            }
        }

        // GET: api/Farmers/5/products
        [HttpGet("{id}/products")]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetFarmerProducts(int id)
        {
            try
            {
                var farmer = await _context.Farmers
                    .Include(f => f.Products)
                    .FirstOrDefaultAsync(f => f.FarmerId == id);

                if (farmer == null)
                {
                    return NotFound(new { message = "Farmer not found" });
                }

                var productDtos = farmer.Products?.Select(p => new ProductDto
                {
                    Id = p.ProductId,
                    Name = p.Name,
                    Description = p.Description,
                    Category = p.Category,
                    Price = p.Price,
                    QuantityAvailable = p.QuantityAvailable,
                    ProductionDate = p.ProductionDate,
                    FarmerName = farmer.Name,
                    FarmerId = farmer.FarmerId
                }).ToList() ?? new List<ProductDto>();

                return Ok(productDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products for farmer {FarmerId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving farmer products" });
            }
        }
    }

    // DTOs for API
    public class FarmerDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string FarmName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public string UserId { get; set; } = string.Empty;
    }

    public class FarmerDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string FarmName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public List<ProductSummaryDto> Products { get; set; } = new List<ProductSummaryDto>();
    }

    public class ProductSummaryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int QuantityAvailable { get; set; }
        public DateTime ProductionDate { get; set; }
    }

    public class CreateFarmerDto
    {
        public string Name { get; set; } = string.Empty;
        public string FarmName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    public class UpdateFarmerDto
    {
        public string? Name { get; set; }
        public string? FarmName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
    }

} 