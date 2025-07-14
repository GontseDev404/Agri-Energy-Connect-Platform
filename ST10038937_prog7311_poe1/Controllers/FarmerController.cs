using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ST10038937_prog7311_poe1.Data;
using ST10038937_prog7311_poe1.Models;
using System.Security.Claims;
using ST10038937_prog7311_poe1.Services;
using Microsoft.Extensions.Caching.Memory;

namespace ST10038937_prog7311_poe1.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class FarmerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IAuditService _auditService;
        private readonly IMemoryCache _cache;

        public FarmerController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IAuditService auditService, IMemoryCache cache)
        {
            _context = context;
            _userManager = userManager;
            _auditService = auditService;
            _cache = cache;
        }

        // GET: Farmer
        public async Task<IActionResult> Index(string search)
        {
            string cacheKey = $"farmers_{search}";
            if (!_cache.TryGetValue(cacheKey, out List<Farmer> farmers))
            {
                var farmersQuery = _context.Farmers.AsNoTracking().AsQueryable();
                if (!string.IsNullOrWhiteSpace(search))
                {
                    farmersQuery = farmersQuery.Where(f => f.Name.Contains(search) || f.FarmName.Contains(search) || f.Email.Contains(search));
                }
                farmers = await farmersQuery.ToListAsync();
                _cache.Set(cacheKey, farmers, TimeSpan.FromMinutes(1));
            }
            ViewBag.Search = search;
            return View(farmers);
        }

        // GET: Farmer/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var farmer = await _context.Farmers
                .Include(f => f.Products)
                .FirstOrDefaultAsync(m => m.FarmerId == id);
                
            if (farmer == null)
            {
                return NotFound();
            }

            return View(farmer);
        }

        // GET: Farmer/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Farmer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,FarmName,Address,PhoneNumber,Email")] Farmer farmer)
        {
            if (ModelState.IsValid)
            {
                // Create a new user account for the farmer
                var user = new ApplicationUser
                {
                    UserName = farmer.Email,
                    Email = farmer.Email,
                    EmailConfirmed = true,
                    FirstName = farmer.Name.Split(' ')[0],
                    LastName = farmer.Name.Contains(' ') ? farmer.Name.Substring(farmer.Name.IndexOf(' ') + 1) : "",
                    UserRole = "Farmer"
                };

                // Generate a temporary password
                var tempPassword = "Farmer1!";
                var result = await _userManager.CreateAsync(user, tempPassword);

                if (result.Succeeded)
                {
                    // Assign the Farmer role
                    await _userManager.AddToRoleAsync(user, "Farmer");
                    
                    // Link the farmer profile to the user account
                    farmer.UserId = user.Id;
                    
                                        _context.Add(farmer);
                    await _context.SaveChangesAsync();

                    // Audit log
                    var currentUser = await _userManager.GetUserAsync(User);
                    if (currentUser != null) {
                        await _auditService.LogActionAsync(currentUser.Id, "Create Farmer", $"Farmer ID: {farmer.FarmerId}, Name: {farmer.Name}");
                    }
                    
                    TempData["SuccessMessage"] = $"Farmer created successfully. Temporary password: {tempPassword}";
                    return RedirectToAction(nameof(Index));
                }
                
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            
            return View(farmer);
        }

        // GET: Farmer/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var farmer = await _context.Farmers.FindAsync(id);
            if (farmer == null)
            {
                return NotFound();
            }
            
            return View(farmer);
        }

        // POST: Farmer/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("FarmerId,Name,FarmName,Address,PhoneNumber,Email,UserId")] Farmer farmer)
        {
            if (id != farmer.FarmerId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(farmer);
                    await _context.SaveChangesAsync();
                    
                    // Update the associated user account
                    var user = await _userManager.FindByIdAsync(farmer.UserId);
                    if (user != null)
                    {
                        user.Email = farmer.Email;
                        user.UserName = farmer.Email;
                        user.FirstName = farmer.Name.Split(' ')[0];
                        user.LastName = farmer.Name.Contains(' ') ? farmer.Name.Substring(farmer.Name.IndexOf(' ') + 1) : "";
                        
                                                await _userManager.UpdateAsync(user);
                    }

                    // Audit log
                    var currentUser = await _userManager.GetUserAsync(User);
                    if (currentUser != null) {
                        await _auditService.LogActionAsync(currentUser.Id, "Edit Farmer", $"Farmer ID: {farmer.FarmerId}, Name: {farmer.Name}");
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!FarmerExists(farmer.FarmerId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(farmer);
        }

        // GET: Farmer/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var farmer = await _context.Farmers
                .FirstOrDefaultAsync(m => m.FarmerId == id);
            if (farmer == null)
            {
                return NotFound();
            }

            return View(farmer);
        }

        // POST: Farmer/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var farmer = await _context.Farmers.FindAsync(id);
            
            if (farmer != null)
            {
                // Delete the associated user account
                var user = await _userManager.FindByIdAsync(farmer.UserId);
                if (user != null)
                {
                    await _userManager.DeleteAsync(user);
                }
                
                                var farmerName = farmer.Name;
                var farmerId = farmer.FarmerId;

                _context.Farmers.Remove(farmer);
                await _context.SaveChangesAsync();

                // Audit log
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null) {
                    await _auditService.LogActionAsync(currentUser.Id, "Delete Farmer", $"Farmer ID: {farmerId}, Name: {farmerName}");
                }
            }
            
            return RedirectToAction(nameof(Index));
        }

        private bool FarmerExists(int id)
        {
            return _context.Farmers.Any(e => e.FarmerId == id);
        }
    }
}
