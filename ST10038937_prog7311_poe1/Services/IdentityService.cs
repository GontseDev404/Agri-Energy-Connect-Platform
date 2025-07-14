using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ST10038937_prog7311_poe1.Models;

namespace ST10038937_prog7311_poe1.Services
{
    public static class IdentityService
    {
        public static IServiceCollection AddCustomIdentity(this IServiceCollection services)
        {
            services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                // Password settings - Enhanced security
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 12; // Increased from 8
                options.Password.RequiredUniqueChars = 3; // Increased from 1

                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User settings
                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789._-";
                options.SignIn.RequireConfirmedEmail = false; // Set to true in production
                options.SignIn.RequireConfirmedPhoneNumber = false;

                // Token settings
                options.Tokens.ChangeEmailTokenProvider = TokenOptions.DefaultEmailProvider;
                options.Tokens.ChangePhoneNumberTokenProvider = TokenOptions.DefaultPhoneProvider;
                options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;
                options.Tokens.AuthenticatorTokenProvider = TokenOptions.DefaultAuthenticatorProvider;
            })
            .AddEntityFrameworkStores<Data.ApplicationDbContext>()
            .AddDefaultTokenProviders()
            .AddDefaultUI();

            // Configure password hashing
            services.Configure<PasswordHasherOptions>(options =>
            {
                options.IterationCount = 100000; // Increased from default for better security
                options.CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3;
            });

            // Register custom application services
            services.AddScoped<IAuditService, AuditService>();

            return services;
        }
    }
}
