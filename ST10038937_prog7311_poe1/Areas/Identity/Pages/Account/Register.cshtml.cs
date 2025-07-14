using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using ST10038937_prog7311_poe1.Data;
using ST10038937_prog7311_poe1.Models;
using ST10038937_prog7311_poe1.Services;

namespace ST10038937_prog7311_poe1.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            IAuditService auditService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _roleManager = roleManager;
            _context = context;
            _auditService = auditService;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new InputModel();

        public string ReturnUrl { get; set; } = string.Empty;

        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

        public class InputModel
        {
            [Required]
            [Display(Name = "First Name")]
            public string FirstName { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Last Name")]
            public string LastName { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Email Username")]
            public string EmailUsername { get; set; } = string.Empty;

            [Required]
            [Display(Name = "Role")]
            public string Role { get; set; } = string.Empty;

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; } = string.Empty;

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; } = string.Empty;
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            ReturnUrl = returnUrl ?? string.Empty;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl = string.IsNullOrEmpty(returnUrl) ? Url.Content("~/") : returnUrl;
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            
            if (ModelState.IsValid)
            {
                // Generate email based on role selection
                string domainSuffix = string.Equals(Input.Role, "farmer", StringComparison.OrdinalIgnoreCase) ? "farmer.com" : "agrienergy.com";
                string email = $"{Input.EmailUsername}@{domainSuffix}";

                // Check if email already exists
                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null)
                {
                    await _auditService.LogActionAsync(existingUser.Id, "Registration Failed - Email Exists", $"Email: {email}");
                    ModelState.AddModelError(string.Empty, "Email already exists. Please choose a different username.");
                    return Page();
                }
                
                try {
                    var user = new ApplicationUser
                    {
                        UserName = email,
                        Email = email,
                        FirstName = Input.FirstName,
                        LastName = Input.LastName,
                        UserRole = Input.Role,
                        EmailConfirmed = true // Auto-confirm for demo purposes
                    };
                    
                    var result = await _userManager.CreateAsync(user, Input.Password);
                    
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User created a new account with password.");
                        await _auditService.LogActionAsync(user.Id, "Registration Success", $"Email: {email}, Role: {Input.Role}");

                    // Add user to the role
                    if (!await _roleManager.RoleExistsAsync(Input.Role))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(Input.Role));
                    }
                    
                    await _userManager.AddToRoleAsync(user, Input.Role);

                    // If this is a farmer, create the farmer profile too
                    if (Input.Role == "Farmer")
                    {
                        var farmer = new Farmer
                        {
                            Name = $"{Input.FirstName} {Input.LastName}",
                            FarmName = $"{Input.LastName} Farm", // Default farm name
                            Email = email,
                            Address = "Enter address here",
                            PhoneNumber = "+27 83 000 0000",
                            UserId = user.Id,
                            Products = new List<Product>()
                        };
                        
                        _context.Farmers.Add(farmer);
                        await _context.SaveChangesAsync();
                    }

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl ?? Url.Content("~/"));
                    }
                    else
                    {
                        foreach (var error in result.Errors)
                        {
                            ModelState.AddModelError(string.Empty, error.Description);
                        }
                        await _auditService.LogActionAsync("(unknown)", "Registration Failed - Identity Error", $"Email: {email}, Errors: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                        return Page();
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception
                    _logger.LogError(ex, "Error during registration process");
                    await _auditService.LogActionAsync("(unknown)", "Registration Failed - Exception", $"Email: {email}, Exception: {ex.Message}");
                    ModelState.AddModelError(string.Empty, "An error occurred during registration. Please try again later.");
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
