using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ST10038937_prog7311_poe1.Data;
using Microsoft.Extensions.Caching.Memory;

namespace ST10038937_prog7311_poe1.Controllers
{
    [Authorize(Roles = "Admin,Employee")]
    public class AuditLogsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;

        public AuditLogsController(ApplicationDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
        }

        public async Task<IActionResult> Index(string? userId, string? action, DateTime? startDate, DateTime? endDate)
        {
            string cacheKey = $"auditLogs_{userId}_{action}_{startDate}_{endDate}";
            if (!_cache.TryGetValue(cacheKey, out List<ST10038937_prog7311_poe1.Models.AuditLog> auditLogsList))
            {
                var auditLogs = _context.AuditLogs.AsNoTracking().AsQueryable();

                if (!string.IsNullOrEmpty(userId))
                {
                    auditLogs = auditLogs.Where(a => a.UserId == userId);
                }

                if (!string.IsNullOrEmpty(action))
                {
                    auditLogs = auditLogs.Where(a => a.Action != null && a.Action.Contains(action));
                }

                if (startDate.HasValue)
                {
                    auditLogs = auditLogs.Where(a => a.Timestamp >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    auditLogs = auditLogs.Where(a => a.Timestamp <= endDate.Value);
                }
                auditLogsList = await auditLogs.OrderByDescending(a => a.Timestamp).ToListAsync();
                _cache.Set(cacheKey, auditLogsList, TimeSpan.FromMinutes(1));
            }
            ViewBag.UserId = userId;
            ViewBag.Action = action;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            return View(auditLogsList);
        }
    }
}
