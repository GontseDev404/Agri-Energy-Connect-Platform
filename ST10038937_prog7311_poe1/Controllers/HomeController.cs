using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using ST10038937_prog7311_poe1.Data;
using ST10038937_prog7311_poe1.Models;
using ST10038937_prog7311_poe1.Services;

namespace ST10038937_prog7311_poe1.Controllers;

using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Http;
using System.Globalization;
using Microsoft.Extensions.Localization;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStringLocalizer<HomeController> _localizer;
    private readonly IAuditService _auditService;
    private readonly IDatabaseOptimizationService _dbOptimizationService;

    public HomeController(
        ILogger<HomeController> logger, 
        ApplicationDbContext context, 
        UserManager<ApplicationUser> userManager,
        IStringLocalizer<HomeController> localizer,
        IAuditService auditService,
        IDatabaseOptimizationService dbOptimizationService)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _localizer = localizer;
        _auditService = auditService;
        _dbOptimizationService = dbOptimizationService;
    }

    public async Task<IActionResult> Index()
    {
        // If user is authenticated, show role-specific dashboard
        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var user = await _userManager.GetUserAsync(User);
            
            if (User.IsInRole("Farmer"))
            {
                // Get farmer profile with caching
                var farmer = user != null ? await _dbOptimizationService.GetCachedSingleAsync(
                    $"farmer_{user.Id}",
                    async () => await _context.Farmers.FirstOrDefaultAsync(f => f.UserId == user.Id),
                    TimeSpan.FromMinutes(10)
                ) : null;
                
                if (farmer != null)
                {
                    // Get farmer's products with caching
                    var products = await _dbOptimizationService.GetCachedDataAsync(
                        $"farmer_products_{farmer.FarmerId}",
                        async () => await _context.Products
                            .Where(p => p.FarmerId == farmer.FarmerId)
                            .AsNoTracking()
                            .ToListAsync(),
                        TimeSpan.FromMinutes(5)
                    );
                    
                    ViewBag.Farmer = farmer;
                    ViewBag.ProductCount = products.Count;
                    ViewBag.Categories = products
                        .Select(p => p.Category)
                        .Distinct()
                        .ToList();
                    
                    return View("FarmerDashboard");
                }
            }
            else if (User.IsInRole("Employee"))
            {
                // Get dashboard statistics with caching
                var stats = await _dbOptimizationService.GetDashboardStatsAsync();
                
                ViewBag.FarmerCount = stats.GetValueOrDefault("Farmers", 0);
                ViewBag.ProductCount = stats.GetValueOrDefault("Products", 0);
                
                // Get categories with caching
                var categories = await _dbOptimizationService.GetCachedDataAsync(
                    "product_categories",
                    async () => await _context.Products
                        .Select(p => p.Category)
                        .Distinct()
                        .AsNoTracking()
                        .ToListAsync(),
                    TimeSpan.FromMinutes(10)
                );
                ViewBag.Categories = categories;
                
                // Get recent products with caching
                var recentProducts = await _dbOptimizationService.GetRecentDataAsync(
                    _context.Products
                        .Include(p => p.Farmer)
                        .OrderByDescending(p => p.ProductionDate),
                    5,
                    "recent_products"
                );
                ViewBag.RecentProducts = recentProducts;
                
                return View("EmployeeDashboard");
            }
        }
        
        // Default landing page for unauthenticated users
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }
    
    public IActionResult About()
    {
        return View();
    }
    
    public IActionResult ApiDocumentation()
    {
        return View();
    }
    
    [HttpPost]
    public IActionResult SetLanguage(string culture, string returnUrl = "/")
    {
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true }
        );

        // For AJAX calls, we don't want to redirect.
        // We can check if the request is an AJAX request.
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Ok();
        }

        return LocalRedirect(returnUrl);
    }

    [HttpGet]
    public IActionResult GetTranslations([FromQuery] string[] keys)
    {
        if (keys == null || !keys.Any())
        {
            return BadRequest("No keys provided.");
        }

        var translations = keys.ToDictionary(key => key, key => _localizer[key].Value);
        return new JsonResult(translations);
    }
    
    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        var user = await _userManager.GetUserAsync(User);
        string userId = user?.Id ?? "(unknown)";
        await _auditService.LogActionAsync(userId, "Logout", $"User logged out");
        await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
        return RedirectToAction("Index", "Home");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
