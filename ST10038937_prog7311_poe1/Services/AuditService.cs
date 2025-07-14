using ST10038937_prog7311_poe1.Data;
using ST10038937_prog7311_poe1.Models;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;

namespace ST10038937_prog7311_poe1.Services
{
    public interface IAuditService
    {
        Task LogActionAsync(string userId, string action, string? details = null);
        Task LogSecurityEventAsync(string userId, string eventType, string details, string ipAddress);
        Task LogUserActivityAsync(string userId, string activity, string? ipAddress = null, string? userAgent = null);
        Task LogDataAccessAsync(string userId, string entityType, string entityId, string operation, string? details = null);
        Task LogConfigurationChangeAsync(string userId, string setting, string oldValue, string newValue);
        Task<List<AuditLog>> GetUserActivityAsync(string userId, DateTime? startDate = null, DateTime? endDate = null);
        Task<List<AuditLog>> GetSecurityEventsAsync(DateTime? startDate = null, DateTime? endDate = null);
        Task<List<AuditLog>> GetDataAccessLogsAsync(string entityType, string entityId);
        Task<Dictionary<string, int>> GetAuditStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
    }

    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuditService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(
            ApplicationDbContext context, 
            ILogger<AuditService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogActionAsync(string userId, string action, string? details = null)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(action))
            {
                return; 
            }

            var ipAddress = GetClientIpAddress();
            var userAgent = GetUserAgent();

            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = action,
                Details = details,
                Timestamp = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Audit log: User {UserId} performed {Action} from IP {IpAddress}", 
                userId, action, ipAddress);
        }

        public async Task LogSecurityEventAsync(string userId, string eventType, string details, string ipAddress)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(eventType))
            {
                return;
            }

            var userAgent = GetUserAgent();

            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = $"Security: {eventType}",
                Details = $"{details} | IP: {ipAddress} | UserAgent: {userAgent}",
                Timestamp = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsSecurityEvent = true
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogWarning("Security event: User {UserId} - {EventType} from IP {IpAddress}", 
                userId, eventType, ipAddress);
        }

        public async Task LogUserActivityAsync(string userId, string activity, string? ipAddress = null, string? userAgent = null)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(activity))
            {
                return;
            }

            ipAddress ??= GetClientIpAddress();
            userAgent ??= GetUserAgent();

            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = $"Activity: {activity}",
                Details = $"IP: {ipAddress} | UserAgent: {userAgent}",
                Timestamp = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogDebug("User activity: User {UserId} - {Activity} from IP {IpAddress}", 
                userId, activity, ipAddress);
        }

        public async Task LogDataAccessAsync(string userId, string entityType, string entityId, string operation, string? details = null)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(entityType) || string.IsNullOrEmpty(entityId))
            {
                return;
            }

            var ipAddress = GetClientIpAddress();
            var userAgent = GetUserAgent();

            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = $"Data Access: {operation}",
                Details = $"Entity: {entityType} | ID: {entityId} | {details} | IP: {ipAddress}",
                Timestamp = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                EntityType = entityType,
                EntityId = entityId
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Data access: User {UserId} {Operation} {EntityType} {EntityId} from IP {IpAddress}", 
                userId, operation, entityType, entityId, ipAddress);
        }

        public async Task LogConfigurationChangeAsync(string userId, string setting, string oldValue, string newValue)
        {
            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(setting))
            {
                return;
            }

            var ipAddress = GetClientIpAddress();
            var userAgent = GetUserAgent();

            var auditLog = new AuditLog
            {
                UserId = userId,
                Action = "Configuration Change",
                Details = $"Setting: {setting} | Old: {oldValue} | New: {newValue} | IP: {ipAddress}",
                Timestamp = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsSecurityEvent = true
            };

            _context.AuditLogs.Add(auditLog);
            await _context.SaveChangesAsync();

            _logger.LogWarning("Configuration change: User {UserId} changed {Setting} from {OldValue} to {NewValue} from IP {IpAddress}", 
                userId, setting, oldValue, newValue, ipAddress);
        }

        public async Task<List<AuditLog>> GetUserActivityAsync(string userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.AuditLogs.Where(a => a.UserId == userId);

            if (startDate.HasValue)
            {
                query = query.Where(a => a.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(a => a.Timestamp <= endDate.Value);
            }

            return await query
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();
        }

        public async Task<List<AuditLog>> GetSecurityEventsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.AuditLogs.Where(a => a.IsSecurityEvent);

            if (startDate.HasValue)
            {
                query = query.Where(a => a.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(a => a.Timestamp <= endDate.Value);
            }

            return await query
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();
        }

        public async Task<List<AuditLog>> GetDataAccessLogsAsync(string entityType, string entityId)
        {
            return await _context.AuditLogs
                .Where(a => a.EntityType == entityType && a.EntityId == entityId)
                .OrderByDescending(a => a.Timestamp)
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetAuditStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(a => a.Timestamp >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(a => a.Timestamp <= endDate.Value);
            }

            var stats = new Dictionary<string, int>();

            // Total audit logs
            stats["TotalLogs"] = await query.CountAsync();

            // Security events
            stats["SecurityEvents"] = await query.CountAsync(a => a.IsSecurityEvent);

            // Data access events
            stats["DataAccessEvents"] = await query.CountAsync(a => !string.IsNullOrEmpty(a.EntityType));

            // Unique users
            stats["UniqueUsers"] = await query.Select(a => a.UserId).Distinct().CountAsync();

            // Unique IP addresses
            stats["UniqueIPs"] = await query.Where(a => !string.IsNullOrEmpty(a.IpAddress))
                .Select(a => a.IpAddress).Distinct().CountAsync();

            return stats;
        }

        private string GetClientIpAddress()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return "Unknown";

            // Check for forwarded headers (for proxy/load balancer scenarios)
            var forwardedHeader = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedHeader))
            {
                return forwardedHeader.Split(',')[0].Trim();
            }

            var realIpHeader = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIpHeader))
            {
                return realIpHeader;
            }

            return httpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        }

        private string GetUserAgent()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            return httpContext?.Request.Headers["User-Agent"].FirstOrDefault() ?? "Unknown";
        }
    }
}
