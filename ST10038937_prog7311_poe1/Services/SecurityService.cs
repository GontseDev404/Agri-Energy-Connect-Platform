using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ST10038937_prog7311_poe1.Data;
using ST10038937_prog7311_poe1.Models;
using System.Text.RegularExpressions;

namespace ST10038937_prog7311_poe1.Services
{
    public interface ISecurityService
    {
        Task<SecurityValidationResult> ValidatePasswordAsync(string password, string? userName = null);
        Task<SecurityValidationResult> ValidateUsernameAsync(string userName);
        Task<bool> IsAccountLockedAsync(string userName);
        Task<int> GetFailedLoginAttemptsAsync(string userName);
        Task RecordFailedLoginAttemptAsync(string userName, string ipAddress);
        Task ResetFailedLoginAttemptsAsync(string userName);
        Task<bool> IsPasswordExpiredAsync(string userName);
        Task<DateTime?> GetPasswordLastChangedAsync(string userName);
        Task<bool> IsPasswordReusedAsync(string userName, string newPassword);
        Task<List<string>> GetSecurityRecommendationsAsync(string userName);
        Task<bool> IsSuspiciousActivityAsync(string userName, string action, string ipAddress);
        Task LogSecurityEventAsync(string userName, string eventType, string details, string ipAddress);
    }

    public class SecurityService : ISecurityService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly ILogger<SecurityService> _logger;
        private readonly IConfiguration _configuration;

        public SecurityService(
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IAuditService auditService,
            ILogger<SecurityService> logger,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _context = context;
            _auditService = auditService;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<SecurityValidationResult> ValidatePasswordAsync(string password, string? userName = null)
        {
            var result = new SecurityValidationResult { IsValid = true };

            // Check minimum length
            if (password.Length < 12)
            {
                result.IsValid = false;
                result.Errors.Add("Password must be at least 12 characters long.");
            }

            // Check for uppercase letters
            if (!password.Any(char.IsUpper))
            {
                result.IsValid = false;
                result.Errors.Add("Password must contain at least one uppercase letter.");
            }

            // Check for lowercase letters
            if (!password.Any(char.IsLower))
            {
                result.IsValid = false;
                result.Errors.Add("Password must contain at least one lowercase letter.");
            }

            // Check for digits
            if (!password.Any(char.IsDigit))
            {
                result.IsValid = false;
                result.Errors.Add("Password must contain at least one digit.");
            }

            // Check for special characters
            if (!password.Any(c => !char.IsLetterOrDigit(c)))
            {
                result.IsValid = false;
                result.Errors.Add("Password must contain at least one special character.");
            }

            // Check for common patterns
            if (IsCommonPassword(password))
            {
                result.IsValid = false;
                result.Errors.Add("Password cannot be a commonly used password.");
            }

            // Check for sequential characters
            if (HasSequentialCharacters(password))
            {
                result.IsValid = false;
                result.Errors.Add("Password cannot contain sequential characters (e.g., 123, abc).");
            }

            // Check for repeated characters
            if (HasRepeatedCharacters(password))
            {
                result.IsValid = false;
                result.Errors.Add("Password cannot contain repeated characters (e.g., aaa, 111).");
            }

            // Check if password contains username
            if (!string.IsNullOrEmpty(userName) && password.ToLower().Contains(userName.ToLower()))
            {
                result.IsValid = false;
                result.Errors.Add("Password cannot contain your username.");
            }

            // Check for password reuse if user exists
            if (!string.IsNullOrEmpty(userName))
            {
                var user = await _userManager.FindByNameAsync(userName);
                if (user != null && await IsPasswordReusedAsync(userName, password))
                {
                    result.IsValid = false;
                    result.Errors.Add("Password cannot be the same as your previous passwords.");
                }
            }

            return result;
        }

        public async Task<SecurityValidationResult> ValidateUsernameAsync(string userName)
        {
            var result = new SecurityValidationResult { IsValid = true };

            // Check minimum length
            if (userName.Length < 3)
            {
                result.IsValid = false;
                result.Errors.Add("Username must be at least 3 characters long.");
            }

            // Check maximum length
            if (userName.Length > 50)
            {
                result.IsValid = false;
                result.Errors.Add("Username cannot exceed 50 characters.");
            }

            // Check for valid characters
            if (!Regex.IsMatch(userName, @"^[a-zA-Z0-9._-]+$"))
            {
                result.IsValid = false;
                result.Errors.Add("Username can only contain letters, numbers, dots, underscores, and hyphens.");
            }

            // Check for reserved words
            var reservedWords = new[] { "admin", "administrator", "root", "system", "guest", "test", "demo" };
            if (reservedWords.Contains(userName.ToLower()))
            {
                result.IsValid = false;
                result.Errors.Add("Username cannot be a reserved word.");
            }

            return result;
        }

        public async Task<bool> IsAccountLockedAsync(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null) return false;

            return await _userManager.IsLockedOutAsync(user);
        }

        public async Task<int> GetFailedLoginAttemptsAsync(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null) return 0;

            return await _userManager.GetAccessFailedCountAsync(user);
        }

        public async Task RecordFailedLoginAttemptAsync(string userName, string ipAddress)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null) return;

            await _userManager.AccessFailedAsync(user);
            await _auditService.LogActionAsync(user.Id, "Failed Login Attempt", $"IP: {ipAddress}");
            
            _logger.LogWarning("Failed login attempt for user {UserName} from IP {IpAddress}", userName, ipAddress);
        }

        public async Task ResetFailedLoginAttemptsAsync(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null) return;

            await _userManager.ResetAccessFailedCountAsync(user);
        }

        public async Task<bool> IsPasswordExpiredAsync(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null) return false;

            var lastChanged = await GetPasswordLastChangedAsync(userName);
            if (!lastChanged.HasValue) return false;

            var maxAgeDays = _configuration.GetValue<int>("Security:PasswordMaxAgeDays", 90);
            return DateTime.UtcNow.Subtract(lastChanged.Value).TotalDays > maxAgeDays;
        }

        public async Task<DateTime?> GetPasswordLastChangedAsync(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null) return null;

            // This would need to be implemented with a custom user store
            // For now, return null to indicate no tracking
            return null;
        }

        public async Task<bool> IsPasswordReusedAsync(string userName, string newPassword)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null) return false;

            // This would need to be implemented with password history tracking
            // For now, return false to allow password reuse
            return false;
        }

        public async Task<List<string>> GetSecurityRecommendationsAsync(string userName)
        {
            var recommendations = new List<string>();
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null) return recommendations;

            // Check if email is confirmed
            if (!user.EmailConfirmed)
            {
                recommendations.Add("Confirm your email address for enhanced security.");
            }

            // Check if two-factor authentication is enabled
            var hasTwoFactor = await _userManager.GetTwoFactorEnabledAsync(user);
            if (!hasTwoFactor)
            {
                recommendations.Add("Enable two-factor authentication for additional security.");
            }

            // Check password age
            if (await IsPasswordExpiredAsync(userName))
            {
                recommendations.Add("Your password has expired. Please change it immediately.");
            }

            // Check for failed login attempts
            var failedAttempts = await GetFailedLoginAttemptsAsync(userName);
            if (failedAttempts > 0)
            {
                recommendations.Add($"You have {failedAttempts} failed login attempts. Consider changing your password.");
            }

            return recommendations;
        }

        public async Task<bool> IsSuspiciousActivityAsync(string userName, string action, string ipAddress)
        {
            // Check for multiple failed login attempts
            var failedAttempts = await GetFailedLoginAttemptsAsync(userName);
            if (failedAttempts >= 5)
            {
                return true;
            }

            // Check for rapid successive actions
            var recentActions = await _context.AuditLogs
                .Where(a => a.UserId == userName && a.Timestamp >= DateTime.UtcNow.AddMinutes(5))
                .CountAsync();

            if (recentActions > 20)
            {
                return true;
            }

            // Check for actions from unusual IP addresses
            // This would require IP geolocation service in production

            return false;
        }

        public async Task LogSecurityEventAsync(string userName, string eventType, string details, string ipAddress)
        {
            var user = await _userManager.FindByNameAsync(userName);
            var userId = user?.Id ?? userName;

            await _auditService.LogActionAsync(userId, $"Security: {eventType}", $"{details} | IP: {ipAddress}");
            
            _logger.LogInformation("Security event: {EventType} for user {UserName} from IP {IpAddress}", 
                eventType, userName, ipAddress);
        }

        private bool IsCommonPassword(string password)
        {
            var commonPasswords = new[]
            {
                "password", "123456", "123456789", "qwerty", "abc123", "password123",
                "admin", "letmein", "welcome", "monkey", "dragon", "master", "sunshine"
            };

            return commonPasswords.Contains(password.ToLower());
        }

        private bool HasSequentialCharacters(string password)
        {
            for (int i = 0; i < password.Length - 2; i++)
            {
                var c1 = password[i];
                var c2 = password[i + 1];
                var c3 = password[i + 2];

                if (char.IsLetter(c1) && char.IsLetter(c2) && char.IsLetter(c3))
                {
                    if (c2 == c1 + 1 && c3 == c2 + 1) return true;
                }
                else if (char.IsDigit(c1) && char.IsDigit(c2) && char.IsDigit(c3))
                {
                    if (c2 == c1 + 1 && c3 == c2 + 1) return true;
                }
            }

            return false;
        }

        private bool HasRepeatedCharacters(string password)
        {
            for (int i = 0; i < password.Length - 2; i++)
            {
                if (password[i] == password[i + 1] && password[i] == password[i + 2])
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class SecurityValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }
} 